using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class TexturePathExtractionTask : BuildTask<TexturePathExtractionTask.Result>
    {
        private static readonly TimeSpan FileTimeout = TimeSpan.FromSeconds(10);

        public class Result
        {
            public IReadOnlyCollection<string> TexturePaths { get; private init; }

            public Result(IReadOnlyCollection<string> texturePaths)
            {
                TexturePaths = texturePaths;
            }
        }

        public delegate TexturePathExtractionTask Factory(
            PatchSaveTask.Result patch, SharedResourceCopyTask.Result headParts, FaceGenCopyTask.Result faceGen);

        private static readonly Regex TexturePathExpression = new(
            @"[\w\s\p{P}]+\.dds",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private readonly FaceGenCopyTask.Result faceGen;
        private readonly IFileSync fileSync;
        private readonly IFileSystem fs;
        private readonly SharedResourceCopyTask.Result headParts;
        private readonly PatchSaveTask.Result patch;

        public TexturePathExtractionTask(
            IFileSystem fs, IFileSync fileSync, PatchSaveTask.Result patch, SharedResourceCopyTask.Result headParts,
            FaceGenCopyTask.Result faceGen)
        {
            this.faceGen = faceGen;
            this.fileSync = fileSync;
            this.fs = fs;
            this.headParts = headParts;
            this.patch = patch;
        }

        protected override async Task<Result> Run(BuildSettings settings)
        {
            var meshPaths = headParts.MeshPaths.Concat(faceGen.MeshPaths).ToList();
            ItemCount.OnNext(meshPaths.Count);
            var pathsFromTextureSets = patch.Mod.TextureSets
                .SelectMany(x => new[]
                {
                    x.Diffuse,
                    x.NormalOrGloss,
                    x.EnvironmentMaskOrSubsurfaceTint,
                    x.GlowOrDetailMap,
                    x.Height,
                    x.Environment,
                    x.Multilayer,
                    x.BacklightMaskOrSpecular,
                })
                .NotNullOrEmpty()
                .Select(x => x.PrefixPath("textures"));
            var pathsFromMeshes = await meshPaths
                .ThrottledSelect(
                    async path =>
                    {
                        NextItemSync(path);
                        var absolutePath = fs.Path.Combine(settings.OutputDirectory, path);
                        using var fileCts =
                            CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
                        return await Task
                            .Run(() => GetReferencedTexturePaths(absolutePath, fileCts.Token))
                            .WithTimeout(FileTimeout, () => fileCts.Cancel(), CancellationToken);
                    },
                    new ParallelOptions { CancellationToken = CancellationToken })
                .ToListAsync()
                .ConfigureAwait(false);
            var allTexturePaths = pathsFromMeshes
                .SelectMany(p => p)
                .Concat(pathsFromTextureSets)
                .AsParallel()
                .Select(NormalizeTexturePath)
                .ToHashSet(PathComparer.Default);
            return new Result(allTexturePaths);
        }

        private static string? GetPathAfter(string path, string search, int offset)
        {
            var index = path.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + offset) : null;
        }

        // Parsing the NIF using nifly would be more accurate - but this is much faster, and it has never been shown
        // to be inaccurate.
        private async Task<IReadOnlyList<string>> GetReferencedTexturePaths(
            string nifFileName, CancellationToken cancellationToken)
        {
            var texturePaths = new List<string>();
            if (!fs.File.Exists(nifFileName))
                return texturePaths;
            using var _ = fileSync.Lock(nifFileName);
            var fileText = await fs.File.ReadAllTextAsync(nifFileName, cancellationToken)
                .ConfigureAwait(false);
            var match = TexturePathExpression.Match(fileText);
            while (match.Success)
            {
                cancellationToken.ThrowIfCancellationRequested();
                texturePaths.Add(match.Value);
                match = match.NextMatch();
            }
            return texturePaths.ToList();
        }

        private string NormalizeTexturePath(string rawTexturePath)
        {
            var texturePath = rawTexturePath;
            try
            {
                if (fs.Path.IsPathRooted(texturePath))
                {
                    texturePath =
                        GetPathAfter(texturePath, @"data\textures", 5) ??
                        GetPathAfter(texturePath, @"data/textures", 5) ??
                        GetPathAfter(texturePath, @"\textures\", 1) ??
                        GetPathAfter(texturePath, @"/textures\", 1) ??
                        GetPathAfter(texturePath, @"\textures/", 1) ??
                        GetPathAfter(texturePath, @"/textures/", 1) ??
                        GetPathAfter(texturePath, @"\data\", 1) ??
                        GetPathAfter(texturePath, @"/data\", 1) ??
                        GetPathAfter(texturePath, @"\data/", 1) ??
                        GetPathAfter(texturePath, @"/data/", 1) ??
                        texturePath;
                }
            }
            catch (Exception)
            {
                // Just use the best we were able to come up with before the error.
            }
            return texturePath.PrefixPath("textures");
        }
    }
}

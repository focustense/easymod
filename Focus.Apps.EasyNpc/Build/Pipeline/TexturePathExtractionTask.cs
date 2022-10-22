using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class TexturePathExtractionTask : BuildTask<TexturePathExtractionTask.Result>
    {
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
        private readonly PathNormalizer pathNormalizer;

        public TexturePathExtractionTask(
            IFileSystem fs, IFileSync fileSync, PatchSaveTask.Result patch, SharedResourceCopyTask.Result headParts,
            FaceGenCopyTask.Result faceGen)
        {
            this.faceGen = faceGen;
            this.fileSync = fileSync;
            this.fs = fs;
            this.headParts = headParts;
            this.patch = patch;
            pathNormalizer = new(fs);
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
            var extractTasks = meshPaths
                .Select(path =>
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    return Task.Run(() =>
                    {
                        NextItemSync(path);
                        return GetReferencedTexturePaths(fs.Path.Combine(settings.OutputDirectory, path));
                    });
                })
                .ToList();
            var pathsFromMeshes = await Task
                .WhenAll(extractTasks)
                .ConfigureAwait(false);
            var allTexturePaths = pathsFromMeshes
                .SelectMany(p => p)
                .Concat(pathsFromTextureSets)
                .AsParallel()
                .Select(pathNormalizer.NormalizeTexturePath)
                .ToHashSet(PathComparer.Default);
            return new Result(allTexturePaths);
        }

        

        // Parsing the NIF using nifly would be more accurate - but this is much faster, and it has never been shown
        // to be inaccurate.
        private async Task<IReadOnlyList<string>> GetReferencedTexturePaths(string nifFileName)
        {
            var texturePaths = new List<string>();
            if (!fs.File.Exists(nifFileName))
                return texturePaths;
            using var _ = fileSync.Lock(nifFileName);
            var fileText = await fs.File.ReadAllTextAsync(nifFileName).ConfigureAwait(false);
            var match = TexturePathExpression.Match(fileText);
            while (match.Success)
            {
                texturePaths.Add(match.Value);
                match = match.NextMatch();
            }
            return texturePaths.ToList();
        }
    }
}

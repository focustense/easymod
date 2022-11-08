using Focus.Files;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class TexturePathExtractionTask : BuildTask<TexturePathExtractionTask.Result>
    {
        private static readonly IReadOnlyList<string> EmptyTexturePaths = ImmutableList<string>.Empty;

        public class Result
        {
            public IReadOnlyCollection<string> FailedSourcePaths { get; private init; }
            public IReadOnlyCollection<string> TexturePaths { get; private init; }

            public Result(
                IReadOnlyCollection<string> texturePaths,
                IReadOnlyCollection<string> failedSourcePaths)
            {
                TexturePaths = texturePaths;
                FailedSourcePaths = failedSourcePaths;
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
        private readonly ILogger log;
        private readonly PatchSaveTask.Result patch;

        public TexturePathExtractionTask(
            IFileSystem fs, IFileSync fileSync, PatchSaveTask.Result patch, SharedResourceCopyTask.Result headParts,
            FaceGenCopyTask.Result faceGen, ILogger log)
        {
            this.faceGen = faceGen;
            this.fileSync = fileSync;
            this.fs = fs;
            this.headParts = headParts;
            this.log = log;
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
            var failedSourcePaths = new ConcurrentBag<string>();
            var pathsFromMeshes = await meshPaths
                .ThrottledSelect(
                    async path =>
                    {
                        NextItemSync(path);
                        var absolutePath = fs.Path.Combine(settings.OutputDirectory, path);
                        using var fileCts =
                            CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
                        var extractionTask = Task.Run(() => GetReferencedTexturePaths(absolutePath, fileCts.Token));
                        if (settings.TextureExtractionTimeoutSec > 0)
                            extractionTask = extractionTask
                                .WithTimeout(
                                    TimeSpan.FromSeconds(settings.TextureExtractionTimeoutSec),
                                    () => fileCts.Cancel(), CancellationToken)
                                .Catch((TimeoutException ex) =>
                                {
                                    log.Error(
                                        "Extracting texture paths from {meshPath} timed out after {timeout} seconds. " +
                                        "Some textures may be missing from the merge.",
                                        path, settings.TextureExtractionTimeoutSec);
                                    failedSourcePaths.Add(path);
                                    return EmptyTexturePaths;
                                });
                        return await extractionTask;
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
            return new Result(allTexturePaths, failedSourcePaths.ToImmutableList());
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

using Focus.Files;
using Focus.ModManagers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class SharedResourceCopyTask : BuildTask<SharedResourceCopyTask.Result>
    {
        public class Result
        {
            public IReadOnlyCollection<string> FailedPaths { get; private init; }
            public IReadOnlyCollection<string> MeshPaths { get; private init; }
            public IReadOnlyCollection<string> MorphPaths { get; private init; }

            public Result(
                IReadOnlyCollection<string> meshPaths, IReadOnlyCollection<string> morphPaths,
                IReadOnlyCollection<string> failedPaths)
            {
                FailedPaths = failedPaths;
                MeshPaths = meshPaths;
                MorphPaths = morphPaths;
            }
        }

        public delegate SharedResourceCopyTask Factory(PatchSaveTask.Result patch);

        private readonly IFileCopier copier;
        private readonly IModRepository modRepository;
        private readonly PatchSaveTask.Result patch;

        public SharedResourceCopyTask(IFileCopier copier, IModRepository modRepository, PatchSaveTask.Result patch)
        {
            this.copier = copier;
            this.modRepository = modRepository;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                // In general, what we'll want to do for a merge is copy all resources we could possibly need, but NOT
                // vanilla resources, because those are always going to be available anyway.
                //
                // Although it's a tad wasteful CPU-wise, a reasonable way to do this is to first check to see if the
                // file is provided by any active mods at all, and if so, then go through the IFileProvider interface
                // which sets priorities based on actual load order and therefore will always use the "winning file".
                //
                // We can't just use IFileProvider on its own, because it will pick up vanilla assets; and we can't just
                // use the IModRepository on its own, because it isn't fully aware of file priorities.
                var meshPaths = GetHeadPartMeshes()
                    .Concat(GetArmorMeshes())
                    .Concat(GetArmorAddonMeshes())
                    .Concat(GetArtObjectMeshes())
                    .SelectMany(f => GetAllWeights(f))
                    .AsParallel()
                    .Select(f => f.PrefixPath("meshes"))
                    .Where(f => modRepository.ContainsFile(f, true))
                    .ToHashSet(PathComparer.Default);
                var morphPaths = GetHeadPartMorphs()
                    .AsParallel()
                    .Select(f => f.PrefixPath("meshes"))
                    .Where(f => modRepository.ContainsFile(f, true))
                    .ToHashSet(PathComparer.Default);
                ItemCount.OnNext(meshPaths.Count + morphPaths.Count);

                copier.CopyAll(
                    meshPaths, settings.OutputDirectory, NextItemSync, out var failedMeshPaths,
                    CancellationToken);
                copier.CopyAll(
                    morphPaths, settings.OutputDirectory, NextItemSync, out var failedMorphPaths,
                    CancellationToken);
                return new Result(
                    meshPaths, morphPaths,
                    failedMeshPaths.Concat(failedMorphPaths).ToImmutableList());
            });
        }

        private static IEnumerable<string> GetAllWeights(string meshPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(meshPath);
            if (!fileName.EndsWith("_0") && !fileName.EndsWith("_1"))
                return new[] { meshPath };
            var extension = Path.GetExtension(meshPath);
            var directory = Path.GetDirectoryName(meshPath) ?? string.Empty;
            return new[]
            {
                Path.Combine(directory, fileName[0..^1] + "0" + extension),
                Path.Combine(directory, fileName[0..^1] + "1" + extension),
            };
        }

        private IEnumerable<string> GetArmorMeshes()
        {
            return patch.Mod.Armors
                .SelectMany(x => new[]
                {
                    x.WorldModel?.Male?.Model?.File,
                    x.WorldModel?.Male?.Icons?.LargeIconFilename,
                    x.WorldModel?.Male?.Icons?.SmallIconFilename,
                    x.WorldModel?.Female?.Model?.File,
                    x.WorldModel?.Female?.Icons?.LargeIconFilename,
                    x.WorldModel?.Female?.Icons?.SmallIconFilename,
                })
                .NotNull();
        }

        private IEnumerable<string> GetArmorAddonMeshes()
        {
            return patch.Mod.ArmorAddons
                .SelectMany(x => new[]
                {
                    x.FirstPersonModel?.Male?.File,
                    x.FirstPersonModel?.Female?.File,
                    x.WorldModel?.Male?.File,
                    x.WorldModel?.Female?.File,
                })
                .NotNull();
        }

        private IEnumerable<string> GetArtObjectMeshes()
        {
            return patch.Mod.ArtObjects
                .Select(art => art.Model?.File)
                .NotNull();
        }

        private IEnumerable<string> GetHeadPartMeshes()
        {
            return patch.Mod.HeadParts
                .Select(x => x.Model?.File)
                .NotNullOrEmpty();
        }

        private IEnumerable<string> GetHeadPartMorphs()
        {
            return patch.Mod.HeadParts
                .SelectMany(x => x.Parts)
                .Select(x => x.FileName)
                .NotNullOrEmpty();
        }
    }
}

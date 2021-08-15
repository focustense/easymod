using Focus.Files;
using Focus.ModManagers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class HeadPartResourceCopyTask : BuildTask<HeadPartResourceCopyTask.Result>
    {
        public class Result
        {
            public IReadOnlyCollection<string> MeshPaths { get; private init; }
            public IReadOnlyCollection<string> MorphPaths { get; private init; }

            public Result(IReadOnlyCollection<string> meshPaths, IReadOnlyCollection<string> morphPaths)
            {
                MeshPaths = meshPaths;
                MorphPaths = morphPaths;
            }
        }

        public delegate HeadPartResourceCopyTask Factory(PatchSaveTask.Result result);

        private readonly IFileCopier copier;
        private readonly IModRepository modRepository;
        private readonly PatchSaveTask.Result patch;

        public HeadPartResourceCopyTask(IFileCopier copier, IModRepository modRepository, PatchSaveTask.Result patch)
        {
            this.copier = copier;
            this.modRepository = modRepository;
            this.patch = patch;
        }

        public override string Name => "Copy Head Part Meshes/Morphs";

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
                var meshPaths = patch.Mod.HeadParts
                    .AsParallel()
                    .Select(x => x.Model?.File?.PrefixPath("meshes"))
                    .NotNullOrEmpty()
                    .Where(f => modRepository.ContainsFile(f, true))
                    .ToHashSet(PathComparer.Default);
                var morphPaths = patch.Mod.HeadParts
                    .AsParallel()
                    .SelectMany(x => x.Parts)
                    .Select(x => x.FileName?.PrefixPath("meshes"))
                    .NotNullOrEmpty()
                    .Where(f => modRepository.ContainsFile(f, true))
                    .ToHashSet(PathComparer.Default);
                ItemCount.OnNext(meshPaths.Count + morphPaths.Count);

                copier.CopyAll(meshPaths, settings.OutputDirectory, NextItemSync, CancellationToken);
                copier.CopyAll(morphPaths, settings.OutputDirectory, NextItemSync, CancellationToken);
                return new Result(meshPaths, morphPaths);
            });
        }
    }
}

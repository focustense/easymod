using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class FaceGenCopyTask : BuildTask<FaceGenCopyTask.Result>
    {
        public class Result
        {
            public IReadOnlyCollection<string> MeshPaths { get; private init; }
            public IReadOnlyCollection<string> TintPaths { get; private init; }

            public Result(IReadOnlyCollection<string> meshPaths, IReadOnlyCollection<string> tintPaths)
            {
                MeshPaths = meshPaths;
                TintPaths = tintPaths;
            }
        }

        public delegate FaceGenCopyTask Factory();

        public override string Name => "Copy FaceGen Data";

        private readonly IArchiveProvider archiveProvider;
        private readonly IFileSystem fs;
        private readonly IModRepository modRepository;

        public FaceGenCopyTask(IFileSystem fs, IModRepository modRepository, IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;
            this.fs = fs;
            this.modRepository = modRepository;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                var meshPaths = new List<string>();
                var tintPaths = new List<string>();
                var found = settings.Profile.Npcs
                    .AsParallel()
                    .Where(x =>
                        x.FaceGenOverride is not null ||
                        (!x.FaceOption.IsBaseGame && x.FaceOption.PluginName != x.DefaultOption.PluginName))
                    .SelectMany(x =>
                        new[]
                        {
                            FileStructure.GetFaceMeshFileName(x),
                            FileStructure.GetFaceTintFileName(x)
                        }
                        .Select(p => new
                        {
                            Npc = x,
                            Path = p,
                            Components = (x.FaceGenOverride is not null ?
                                x.FaceGenOverride.Components : ComponentsForPlugin(x.FaceOption.PluginName))
                                .ToHashSet(),
                        }))
                    // Mod repository will give us loose first, then archives. If a mod has multiple enabled components
                    // that all have the same facegen as a loose file, then the choice of which one to use is arbitrary
                    // at this time. Is this important enough to track component priorities, with all of the mess that
                    // will create with Vortex (where priorities are actually at the file level)?
                    .Select(x => modRepository
                        .SearchForFiles(x.Path, true)
                        .Where(r => x.Components.Contains(r.ModComponent))
                        .FirstOrDefault())
                    .NotNull()
                    .ToList();
                ItemCount.OnNext(found.Count);
                Parallel.ForEach(found, new ParallelOptions { MaxDegreeOfParallelism = 4 }, x =>
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    NextItemSync(x.RelativePath);
                    var outputPath = fs.Path.Combine(settings.OutputDirectory, x.RelativePath);
                    var wasCopied = false;
                    if (!string.IsNullOrEmpty(x.ArchiveName))
                    {
                        var archivePath = fs.Path.Combine(x.ModComponent.Path, x.ArchiveName);
                        wasCopied = archiveProvider.CopyToFile(archivePath, x.RelativePath, outputPath);
                    }
                    else
                    {
                        var sourcePath = fs.Path.Combine(x.ModComponent.Path, x.RelativePath);
                        try
                        {
                            fs.File.Copy(sourcePath, outputPath);
                            wasCopied = true;
                        }
                        catch (IOException) { }
                    }
                    if (wasCopied)
                    {
                        if (fs.Path.GetExtension(x.RelativePath).Equals(".nif", StringComparison.OrdinalIgnoreCase))
                            lock (meshPaths)
                                meshPaths.Add(x.RelativePath);
                        else
                            lock (tintPaths)
                                tintPaths.Add(x.RelativePath);
                    }
                });
                return new Result(meshPaths, tintPaths);
            });
        }

        private IEnumerable<ModComponentInfo> ComponentsForPlugin(string pluginName)
        {
            return modRepository.SearchForFiles(pluginName, false).Select(x => x.ModComponent);
        }
    }
}

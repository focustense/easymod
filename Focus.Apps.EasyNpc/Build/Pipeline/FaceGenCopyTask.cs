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
            public IReadOnlyCollection<string> FailedPaths { get; private init; }
            public IReadOnlyCollection<string> MeshPaths { get; private init; }
            public IReadOnlyCollection<string> TintPaths { get; private init; }

            public Result(
                IReadOnlyCollection<string> meshPaths, IReadOnlyCollection<string> tintPaths,
                IReadOnlyCollection<string> failedPaths)
            {
                MeshPaths = meshPaths;
                TintPaths = tintPaths;
                FailedPaths = failedPaths;
            }
        }

        public delegate FaceGenCopyTask Factory();

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
                var failedPaths = new List<string>();
                var found = settings.Profile.Npcs
                    .AsParallel()
                    // NPCs using template traits don't need, and generally shouldn't have a facegen. The "chargen data"
                    // (i.e. facegen files) are to be taken from the template.
                    .Where(x => x.DefaultOption.Analysis.TemplateInfo?.InheritsTraits != true)
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
                var outputDirectories = found
                    .Select(x => fs.Path.Combine(settings.OutputDirectory, x.RelativePath))
                    .Select(p => fs.Path.GetDirectoryName(p))
                    .Distinct();
                foreach (var outputDirectory in outputDirectories)
                    fs.Directory.CreateDirectory(outputDirectory);
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
                    else
                        lock (failedPaths)
                            failedPaths.Add(x.RelativePath);
                });
                return new Result(meshPaths, tintPaths, failedPaths);
            });
        }

        private IEnumerable<ModComponentInfo> ComponentsForPlugin(string pluginName)
        {
            return modRepository.SearchForFiles(pluginName, false).Select(x => x.ModComponent);
        }
    }
}

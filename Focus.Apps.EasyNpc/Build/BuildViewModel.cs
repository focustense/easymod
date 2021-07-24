using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profile;
using Focus.ModManagers;
using Focus.Storage.Archives;
using PropertyChanged;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<BuildWarning> WarningExpanded;

        public bool EnableDewiggify { get; set; } = true;
        [DependsOn("Problems")]
        public bool HasProblems => PreBuildReport?.Warnings.Any() ?? false;
        public bool HasWigs { get; private set; }
        public bool IsBuildCompleted { get; private set; }
        [DependsOn("Progress")]
        public bool IsBuilding => Progress != null;
        [DependsOn("OutputModName")]
        public bool IsModOverwriteWarningVisible => ModDirectoryIsNotEmpty(OutputModName);
        public bool IsProblemCheckerEnabled => !IsProblemCheckingInProgress && !IsBuilding && !IsBuildCompleted;
        public bool IsProblemCheckerVisible { get; set; } = true;
        public bool IsProblemCheckingInProgress { get; set; }
        public bool IsProblemReportVisible { get; set; }
        public bool IsReadyToBuild { get; set; }
        [DependsOn("SelectedWarning")]
        public bool IsWarningInfoVisible => SelectedWarning != null;
        public IReadOnlyList<NpcConfiguration<TKey>> Npcs { get; init; }
        public string OutputDirectory { get; private set; }
        public string OutputModName { get; set; } = $"NPC Merge {DateTime.Now:yyyy-MM-dd}";
        public string OutputPluginName => FileStructure.MergeFileName;
        public PreBuildReport PreBuildReport { get; private set; }
        public BuildProgressViewModel Progress { get; private set; }
        public BuildWarning SelectedWarning { get; set; }

        private readonly IArchiveProvider archiveProvider;
        private readonly BuildChecker<TKey> buildChecker;
        private readonly IMergedPluginBuilder<TKey> builder;
        private readonly IFaceGenEditor faceGenEditor;
        private readonly ILogger log;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly IModResolver modResolver;
        private readonly IWigResolver<TKey> wigResolver;

        public BuildViewModel(
            IArchiveProvider archiveProvider, BuildChecker<TKey> buildChecker, IMergedPluginBuilder<TKey> builder,
            IModPluginMapFactory modPluginMapFactory, IModResolver modResolver,
            IEnumerable<NpcConfiguration<TKey>> npcs, IWigResolver<TKey> wigResolver, IFaceGenEditor faceGenEditor,
            ILogger logger)
        {
            this.buildChecker = buildChecker;
            this.builder = builder;
            this.archiveProvider = archiveProvider;
            this.faceGenEditor = faceGenEditor;
            this.log = logger.ForContext<BuildViewModel<TKey>>();
            this.modPluginMapFactory = modPluginMapFactory;
            this.modResolver = modResolver;
            this.wigResolver = wigResolver;
            Npcs = npcs.ToList().AsReadOnly();
        }

        public async void BeginBuild()
        {
            Progress = new BuildProgressViewModel(log);
            await Task.Run(() =>
            {
                // Deleting the build report means we're guaranteed not to have a stale one, but serves the secondary
                // purpose of signaling to parent processes (mod managers/extensions) that the build actually started.
                // This way they can tell the difference between a failed build and no build.
                File.Delete(Settings.Default.BuildReportPath);
                OutputDirectory = Path.Combine(Settings.Default.ModRootDirectory, OutputModName);
                Directory.CreateDirectory(OutputDirectory);
                var buildSettings = GetBuildSettings();
                var mergeInfo = builder.Build(Npcs, buildSettings, Progress.MergedPlugin);
                var modPluginMap = modPluginMapFactory.DefaultMap();
                MergedFolder.Build(
                    Npcs, mergeInfo, archiveProvider, faceGenEditor, modPluginMap, modResolver, buildSettings,
                    Progress.MergedFolder, log);
                BuildArchive();
                new BuildReport { ModName = buildSettings.OutputModName }.SaveToFile(Settings.Default.BuildReportPath);
            }).ConfigureAwait(true);
            IsReadyToBuild = false;
            IsBuildCompleted = true;
        }

        private BuildSettings<TKey> GetBuildSettings()
        {
            return new BuildSettings<TKey>
            {
                EnableDewiggify = EnableDewiggify,
                OutputModName = OutputModName,
                OutputDirectory = OutputDirectory,
                WigResolver = wigResolver,
            };
        }

        // TODO: Add a check for missing textures - requires much deeper inspection of both plugins and meshes.
        public async void CheckForProblems()
        {
            Progress = null;
            IsProblemCheckerVisible = false;
            IsProblemReportVisible = false;
            IsProblemCheckingInProgress = true;
            IsReadyToBuild = false;
            var buildSettings = GetBuildSettings();
            PreBuildReport = await Task.Run(() => buildChecker.CheckAll(Npcs, buildSettings));
            IsProblemCheckingInProgress = false;
            IsProblemReportVisible = true;
        }

        public void DismissProblems()
        {
            IsProblemReportVisible = false;
            IsReadyToBuild = true;
        }

        public void ExpandWarning(BuildWarning warning)
        {
            // There isn't actually any notion of expansion in this context, aside from the info panel which is more of
            // a "select" action. This just serves as a signal for an external component to handle the request.
            WarningExpanded?.Invoke(this, warning);
        }

        public void OpenBuildOutput()
        {
            if (!Directory.Exists(OutputDirectory)) // In case user moved/deleted after the build
                return;
            var psi = new ProcessStartInfo() { FileName = OutputDirectory, UseShellExecute = true };
            Process.Start(psi);
        }

        public void QuickRefresh()
        {
            HasWigs = Npcs.Any(x => x.FaceConfiguration?.Wig != null);
        }

        private void BuildArchive()
        {
            var progress = Progress.Archive;
            progress.StartStage("Packing loose files into archive");
            progress.MaxProgress = 1;
            try
            {
                ArchiveBuilder.BuildResult BuildFilteredArchive(
                    string name, string relativePath, long maxUncompressedSize, int progressWeight = 1)
                {
                    var outputFileName = Path.Combine(OutputDirectory, name) + ".bsa";
                    return new ArchiveBuilder(ArchiveType.SSE)
                        .AddDirectory(Path.Combine(OutputDirectory, relativePath), relativePath)
                        .Compress(true)
                        .ShareData(true)
                        .MaxUncompressedSize(maxUncompressedSize)
                        .OnBeforeBuild(entries =>
                        {
                            // Technically, this addition ISN'T thread safe since it's a read-modify-write op.
                            lock (progress)
                                // Textures are considerably more expensive to add due to the the type of compression,
                                // so applying a higher weight to them avoids "rushing" the progress while the meshes
                                // are running in parallel. We'll still get some rushing due to .NET's Parallel
                                // implementation that creates a large backlog.
                                progress.MaxProgress += (int)Math.Ceiling(entries.Count * progressWeight / 0.99);
                        })
                        .OnPacking(entry => progress.ItemName = $"[{name}.bsa] <- {entry.PathInArchive}")
                        .OnPacked(entry => progress.CurrentProgress += progressWeight)
                        .Build(outputFileName);
                }

                long GB = 1024 * 1024 * 1024;
                var baseName = Path.GetFileNameWithoutExtension(OutputPluginName);
                // Meshes tend to compress at around 50%, so 3 GB should be plenty of headroom.
                var meshesTask = Task.Run(() => BuildFilteredArchive(baseName, "meshes", 3 * GB));
                // Textures can compress to 10% or less of their original size, but don't always assume best-case.
                // A 15 GB limit gives us an expected max compressed size of 1.5 GB, which is the same margin of
                // error as the mesh settings.
                var texturesTask =
                    Task.Run(() => BuildFilteredArchive($"{baseName} - Textures", "textures", 15 * GB, 3));
                Task.WaitAll(meshesTask, texturesTask);
                progress.JumpTo(0.99f);

                progress.StartStage("Adding dummy plugins");
                var archiveFileNames = meshesTask.Result.ArchiveResults
                    .Concat(texturesTask.Result.ArchiveResults)
                    .Select(x => x.FileName);
                foreach (var archiveFileName in archiveFileNames)
                {
                    var archiveBaseName = Path.GetFileNameWithoutExtension(archiveFileName);
                    // Neither the default archive (with same name as merge) nor the standard textures archive need
                    // dummy plugins; the game recognizes these automatically.
                    if (archiveBaseName != baseName && archiveBaseName != $"{baseName} - Textures")
                        builder.CreateDummyPlugin(Path.ChangeExtension(archiveFileName, ".esp"));
                }

                progress.StartStage("Cleaning up loose files");
                Directory.Delete(Path.Combine(OutputDirectory, "meshes"), true);
                Directory.Delete(Path.Combine(OutputDirectory, "textures"), true);

                progress.StartStage("Done");
                progress.CurrentProgress = progress.MaxProgress;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to build BSA archive");
                Progress.Archive.ErrorMessage = ex.Message;
                throw;
            }
        }

        private static bool ModDirectoryIsNotEmpty(string modName)
        {
            var modRootDirectory = Settings.Default.ModRootDirectory;
            var modDirectory = Path.Combine(modRootDirectory, modName);
            return Directory.Exists(modDirectory) && Directory.EnumerateFiles(modDirectory).Any();
        }
    }

    public class BuildProgressViewModel
    {
        public ProgressViewModel Archive { get; init; }
        public ProgressViewModel MergedFolder { get; init; }
        public ProgressViewModel MergedPlugin { get; init; } 

        public BuildProgressViewModel(ILogger logger)
        {
            Archive = new ProgressViewModel(
                "Pack Archive", logger.ForContext("TaskName", "Archive"), true, "Waiting for merged folder");
            MergedPlugin = new ProgressViewModel("Merged Plugin", logger.ForContext("TaskName", "Merged Plugin"));
            MergedFolder = new ProgressViewModel(
                "Merged Folder", logger.ForContext("TaskName", "Merged Folder"), true, "Waiting for merged plugin");
        }
    }
}
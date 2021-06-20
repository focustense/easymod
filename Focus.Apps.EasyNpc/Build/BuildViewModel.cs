using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profile;
using Focus.Storage.Archives;
using PropertyChanged;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool EnableDewiggify { get; set; } = true;
        [DependsOn("Problems")]
        public bool HasProblems => Problems?.Any() ?? false;
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
        public IEnumerable<BuildWarning> Problems { get; private set; }
        public BuildProgressViewModel Progress { get; private set; }
        public BuildWarning SelectedWarning { get; set; }

        private readonly ArchiveFileMap archiveFileMap;
        private readonly IArchiveProvider archiveProvider;
        private readonly IMergedPluginBuilder<TKey> builder;
        private readonly IFaceGenEditor faceGenEditor;
        private readonly ILogger log;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly IWigResolver<TKey> wigResolver;

        public BuildViewModel(
            IArchiveProvider archiveProvider, IMergedPluginBuilder<TKey> builder,
            IModPluginMapFactory modPluginMapFactory, IEnumerable<NpcConfiguration<TKey>> npcs,
            IWigResolver<TKey> wigResolver, IFaceGenEditor faceGenEditor, ArchiveFileMap archiveFileMap, ILogger logger)
        {
            this.archiveFileMap = archiveFileMap;
            this.builder = builder;
            this.archiveProvider = archiveProvider;
            this.faceGenEditor = faceGenEditor;
            this.log = logger.ForContext<BuildViewModel<TKey>>();
            this.modPluginMapFactory = modPluginMapFactory;
            this.wigResolver = wigResolver;
            Npcs = npcs.ToList().AsReadOnly();
        }

        public async void BeginBuild()
        {
            Progress = new BuildProgressViewModel(log);
            await Task.Run(() =>
            {
                OutputDirectory = Path.Combine(Settings.Default.ModRootDirectory, OutputModName);
                Directory.CreateDirectory(OutputDirectory);
                var buildSettings = new BuildSettings<TKey>
                {
                    EnableDewiggify = EnableDewiggify,
                    OutputModName = OutputModName,
                    OutputDirectory = OutputDirectory,
                    WigResolver = wigResolver,
                };
                var mergeInfo = builder.Build(Npcs, buildSettings, Progress.MergedPlugin);
                var modPluginMap = modPluginMapFactory.DefaultMap();
                MergedFolder.Build(
                    Npcs, mergeInfo, archiveProvider, faceGenEditor, modPluginMap, buildSettings, Progress.MergedFolder,
                    log);
                BuildArchive();
            }).ConfigureAwait(true);
            IsReadyToBuild = false;
            IsBuildCompleted = true;
        }

        // TODO: Add a check for missing textures - requires much deeper inspection of both plugins and meshes.
        public async void CheckForProblems()
        {
            Progress = null;
            IsProblemCheckerVisible = false;
            IsProblemReportVisible = false;
            IsProblemCheckingInProgress = true;
            IsReadyToBuild = false;
            var warnings = new List<BuildWarning>();
            await Task.Run(() =>
            {
                warnings.AddRange(CheckModSettings());
                warnings.AddRange(CheckForOverriddenArchives());
                warnings.AddRange(CheckModPluginConsistency());
                warnings.AddRange(CheckWigs());
            });
            var suppressions = GetBuildWarningSuppressions();
            Problems = warnings
                .Where(x =>
                    string.IsNullOrEmpty(x.PluginName) ||
                    x.Id == null ||
                    !suppressions[x.PluginName].Contains((BuildWarningId)x.Id))
                .OrderBy(x => x.Id)
                .ThenBy(x => x.PluginName);
            IsProblemCheckingInProgress = false;
            IsProblemReportVisible = true;
        }

        public void DismissProblems()
        {
            IsProblemReportVisible = false;
            IsReadyToBuild = true;
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

        private IEnumerable<BuildWarning> CheckForOverriddenArchives()
        {
            // It is not - necessarily - a major problem for the game itself if multiple mods provide the same BSA.
            // The game, and this program, will simply use whichever version is actually loaded, i.e. from the last mod
            // in the list. However, there's no obvious way to tell *which* mod is currently providing that BSA.
            // This means that the user might select a mod for some NPC, and we might believe we are pulling facegen
            // data from that mod's archive, but in fact we are pulling it from a different version of the archive
            // provided by some other mod, which might be fine, or might be totally broken.
            // It's also extremely rare, with the only known instance (at the time of writing) being a patch to the
            // Sofia follower mod that removes a conflicting script, i.e. doesn't affect facegen data at all.
            // So it may be an obscure theoretical problem that never comes up in practice, but if we do see it, then
            // it at least merits a warning, which the user can ignore if it's on purpose.
            var modPluginMap = modPluginMapFactory.DefaultMap();
            return archiveProvider.GetLoadedArchivePaths()
                .AsParallel()
                .Select(path => Path.GetFileName(path))
                .Select(f => new
                {
                    Name = f,
                    ProvidingMods = modPluginMap.GetModsForArchive(f).ToList()
                })
                .Where(x => x.ProvidingMods.Count > 1)
                .Select(x => new BuildWarning(
                    BuildWarningId.MultipleArchiveSources,
                    WarningMessages.MultipleArchiveSources(x.Name, x.ProvidingMods)));
        }

        private IEnumerable<BuildWarning> CheckModPluginConsistency()
        {
            archiveFileMap.EnsureInitialized();
            var modPluginMap = modPluginMapFactory.DefaultMap();
            return Npcs.AsParallel()
                .Select(npc => CheckModPluginConsistency(npc, modPluginMap))
                .SelectMany(warnings => warnings);
        }

        private IEnumerable<BuildWarning> CheckModPluginConsistency(
            NpcConfiguration<TKey> npc, ModPluginMap modPluginMap)
        {
            // Our job is to keep NPC records and facegen data consistent. That means we do NOT care about any NPCs that
            // are either still using the master/vanilla plugin as the face source, or have identical face attributes,
            // e.g. UESP records that generally don't touch faces - UNLESS the current profile also uses a facegen data
            // mod for that NPC, which is covered later.
            if (string.IsNullOrEmpty(npc.FaceModName))
            {
                if (npc.RequiresFacegenData())
                    yield return new BuildWarning(
                        BuildWarningId.FaceModNotSpecified,
                        WarningMessages.FaceModNotSpecified(npc.EditorId, npc.Name));
                yield break;
            }

            var modsProvidingFacePlugin = modPluginMap.GetModsForPlugin(npc.FacePluginName);
            if (!modPluginMap.IsModInstalled(npc.FaceModName))
            {
                yield return new BuildWarning(
                    BuildWarningId.FaceModNotInstalled,
                    WarningMessages.FaceModNotInstalled(npc.EditorId, npc.Name, npc.FaceModName));
                yield break;
            }
            if (!modsProvidingFacePlugin.Contains(npc.FaceModName))
                yield return new BuildWarning(
                    npc.FacePluginName,
                    BuildWarningId.FaceModPluginMismatch,
                    WarningMessages.FaceModPluginMismatch(npc.EditorId, npc.Name, npc.FaceModName, npc.FacePluginName));
            var faceMeshFileName = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
            var hasLooseFacegen = File.Exists(
                Path.Combine(Settings.Default.ModRootDirectory, npc.FaceModName, faceMeshFileName));
            var hasArchiveFacegen = modPluginMap.GetArchivesForMod(npc.FaceModName)
                .Select(f => archiveFileMap.ContainsFile(f, faceMeshFileName))
                .Any(exists => exists);
            // If the selected plugin has overrides, then we want to see facegen data. On the other hand, if the
            // selected plugin does NOT have overrides, then a mod providing facegens will probably break something.
            if (npc.RequiresFacegenData() && !hasLooseFacegen && !hasArchiveFacegen)
                // This can mean the mod is missing the facegen, but can also happen if the BSA that would normally
                // include it isn't loaded, i.e. due to the mod or plugin being disabled.
                yield return new BuildWarning(
                    npc.FacePluginName,
                    BuildWarningId.FaceModMissingFaceGen,
                    WarningMessages.FaceModMissingFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
            else if (!npc.RequiresFacegenData() && (hasLooseFacegen || hasArchiveFacegen))
                yield return new BuildWarning(
                    npc.FacePluginName,
                    BuildWarningId.FaceModExtraFaceGen,
                    WarningMessages.FaceModExtraFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
            else if (hasLooseFacegen && hasArchiveFacegen)
                yield return new BuildWarning(
                    npc.FacePluginName,
                    BuildWarningId.FaceModMultipleFaceGen,
                    WarningMessages.FaceModMultipleFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
        }

        private IEnumerable<BuildWarning> CheckModSettings()
        {
            var modRootDirectory = Settings.Default.ModRootDirectory;
            if (string.IsNullOrWhiteSpace(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotSpecified,
                    WarningMessages.ModDirectoryNotSpecified());
            else if (!Directory.Exists(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotFound,
                    WarningMessages.ModDirectoryNotFound(modRootDirectory));
        }

        private IEnumerable<BuildWarning> CheckWigs()
        {
            var wigKeys = Npcs.Select(x => x.FaceConfiguration?.Wig).Where(x => x != null).Distinct();
            var matchedWigKeys = wigResolver.ResolveAll(wigKeys)
                .Where(x => x.HairKeys.Any())
                .Select(x => x.WigKey)
                .ToHashSet();
            return Npcs
                .Select(x => new { Npc = x, x.FaceConfiguration?.Wig })
                .Where(x => x.Wig != null && (!EnableDewiggify || !matchedWigKeys.Contains(x.Wig.Key)))
                .Select(x => EnableDewiggify ?
                    new BuildWarning(
                        x.Wig.IsBald ? BuildWarningId.FaceModWigNotMatchedBald : BuildWarningId.FaceModWigNotMatched,
                        x.Wig.IsBald ?
                            WarningMessages.FaceModWigNotMatchedBald(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.ModelName) :
                            WarningMessages.FaceModWigNotMatched(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.ModelName)
                        ) :
                        new BuildWarning(
                            BuildWarningId.FaceModWigConversionDisabled,
                            WarningMessages.FaceModWigConversionDisabled(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.IsBald)));
        }

        private static ILookup<string, BuildWarningId> GetBuildWarningSuppressions()
        {
            return Settings.Default.BuildWarningWhitelist
                .SelectMany(x => x.IgnoredWarnings.Select(id => new { Plugin = x.PluginName, Id = id }))
                .ToLookup(x => x.Plugin, x => x.Id);
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
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    public class BuildViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [DependsOn("Problems")]
        public bool HasProblems => Problems?.Any() ?? false;
        public bool IsProblemCheckerEnabled => !IsProblemCheckingInProgress;
        public bool IsProblemCheckerVisible { get; set; } = true;
        public bool IsProblemCheckingCompleted { get; set; }
        public bool IsProblemCheckingInProgress { get; set; }
        [DependsOn("SelectedWarning")]
        public bool IsWarningInfoVisible => SelectedWarning != null;
        public IReadOnlyList<NpcConfiguration> Npcs { get; init; }
        public IEnumerable<BuildWarning> Problems { get; private set; }
        public BuildWarning SelectedWarning { get; set; }

        private readonly IReadOnlyList<string> loadOrder;

        public BuildViewModel(IEnumerable<NpcConfiguration> npcs, IReadOnlyList<string> loadOrder)
        {
            Npcs = npcs.ToList().AsReadOnly();
            this.loadOrder = loadOrder;
        }

        // TODO: Add a check for missing textures - requires much deeper inspection of both plugins and meshes.
        public async void CheckForProblems()
        {
            IsProblemCheckerVisible = false;
            IsProblemCheckingCompleted = false;
            IsProblemCheckingInProgress = true;
            var warnings = new List<BuildWarning>();
            await Task.Run(() =>
            {
                warnings.AddRange(CheckModSettings());
                warnings.AddRange(CheckForOverriddenArchives());
                warnings.AddRange(CheckModPluginConsistency());
            });
            var suppressions = GetBuildWarningSuppressions();
            Problems = warnings
                .Where(x =>
                    string.IsNullOrEmpty(x.PluginName) ||
                    x.Id == null ||
                    !suppressions[x.PluginName].Contains((BuildWarningId)x.Id));
            IsProblemCheckingInProgress = false;
            IsProblemCheckingCompleted = true;
        }

        public void DismissProblems()
        {
            IsProblemCheckingCompleted = false;
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
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            return Resources.GetLoadedContainers()
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
            ArchiveFileMap.EnsureInitialized();
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            return Npcs.AsParallel()
                .Select(npc => CheckModPluginConsistency(npc, modPluginMap))
                .SelectMany(warnings => warnings);
        }

        private IEnumerable<BuildWarning> CheckModPluginConsistency(NpcConfiguration npc, ModPluginMap modPluginMap)
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
                Path.Combine(BundlerSettings.Default.ModRootDirectory, npc.FaceModName, faceMeshFileName));
            var hasArchiveFacegen = modPluginMap.GetArchivesForMod(npc.FaceModName)
                .Select(f => ArchiveFileMap.ContainsFile(f, faceMeshFileName))
                .Any();
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
            var modRootDirectory = BundlerSettings.Default.ModRootDirectory;
            if (string.IsNullOrWhiteSpace(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotSpecified,
                    WarningMessages.ModDirectoryNotSpecified());
            else if (!Directory.Exists(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotFound,
                    WarningMessages.ModDirectoryNotFound(modRootDirectory));
        }

        private static ILookup<string, BuildWarningId> GetBuildWarningSuppressions()
        {
            return BundlerSettings.Default.BuildWarningWhitelist
                .Cast<string>()
                .Select(s => s.Split('='))
                .Where(items => items.Length == 2)
                .Select(items => new
                {
                    Plugin = items[0],
                    Warnings = BuildWarningSuppressions.ParseWarnings(items[1])
                })
                .SelectMany(x => x.Warnings.Select(id => new { Plugin = x.Plugin, Id = id }))
                .ToLookup(x => x.Plugin, x => x.Id);
        }
    }
}
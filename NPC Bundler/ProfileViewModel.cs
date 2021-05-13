using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [DependsOn("NpcConfigurations", "OnlyFaceOverrides", "ShowDlcOverrides", "ShowSinglePluginOverrides")]
        public IEnumerable<NpcConfiguration> DisplayedNpcs
        {
            get {
                var minOverrideCount = ShowSinglePluginOverrides ? 1 : 2;
                return npcConfigurations.Values
                    .Where(x => x.GetOverrideCount(ShowDlcOverrides, !OnlyFaceOverrides) >= minOverrideCount);
            }
        }

        [DependsOn("SelectedNpc")]
        public bool HasSelectedNpc => SelectedNpc != null;
        public IReadOnlyList<Mugshot> Mugshots { get; private set; }
        public bool OnlyFaceOverrides { get; set; }
        public NpcOverrideConfiguration SelectedOverrideConfig { get; private set; }
        public NpcConfiguration SelectedNpc { get; private set; }
        public IReadOnlyList<NpcOverrideConfiguration> SelectedNpcOverrides { get; private set; }
        public bool ShowDlcOverrides { get; set; }
        public bool ShowSinglePluginOverrides { get; set; } = true;

        private readonly SortedDictionary<uint, NpcConfiguration> npcConfigurations = new();

        public ProfileViewModel(IEnumerable<Npc> npcs)
        {
            var npcsWithOverrides = npcs.Where(npc => npc.Overrides.Count > 0);
            foreach (var npc in npcsWithOverrides)
                npcConfigurations.Add(npc.FormId, new NpcConfiguration(npc));
        }

        public void SelectMugshot(Mugshot mugshot)
        {
            foreach (var ms in Mugshots ?? Enumerable.Empty<Mugshot>())
                ms.IsHighlighted = false;
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration>())
                overrideConfig.IsHighlighted =
                    mugshot?.ProvidingPlugins?.Contains(overrideConfig.PluginName, StringComparer.OrdinalIgnoreCase) ??
                    false;
        }

        public void SelectNpc(NpcConfiguration npc)
        {
            if (SelectedNpc != null)
                SelectedNpc.FaceModChanged -= OnNpcFaceModChanged;
            ClearNpcHighlights();
            SelectedNpc = npc;
            SelectedNpcOverrides = npc.Overrides;
            Mugshots = Mugshot.GetMugshots(SelectedNpc).ToList().AsReadOnly();
            SyncMugshotMod();
            npc.FaceModChanged += OnNpcFaceModChanged;
        }

        public void SelectOverride(NpcOverrideConfiguration overrideConfig)
        {
            ClearNpcHighlights();
            foreach (var mugshot in Mugshots ?? Enumerable.Empty<Mugshot>())
                mugshot.IsHighlighted =
                    mugshot.ProvidingPlugins.Contains(overrideConfig?.PluginName, StringComparer.OrdinalIgnoreCase);
            if (overrideConfig != null)
                overrideConfig.IsSelected = true;
        }

        public void SetFaceOverride(Mugshot mugshot, bool detectPlugin = false)
        {
            SelectedNpc?.SetFaceMod(mugshot?.ProvidingMod, detectPlugin);
            SyncMugshotMod();
        }

        private void ClearNpcHighlights()
        {
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration>())
                overrideConfig.IsSelected = overrideConfig.IsHighlighted = false;
        }

        private void OnNpcFaceModChanged()
        {
            SyncMugshotMod();
        }

        private void SyncMugshotMod()
        {
            foreach (var ms in Mugshots)
                ms.IsSelectedSource = ms.ProvidingMod == SelectedNpc?.FaceModName;
        }
    }

    public record Mugshot(
        string ProvidingMod, bool IsModInstalled, string[] ProvidingPlugins, string FileName)
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static bool Exists(string modName, string basePluginName, string localFormIdHex)
        {
            var path = Path.Combine(
                BundlerSettings.Default.MugshotsDirectory, modName, basePluginName, $"00{localFormIdHex}.png");
            return File.Exists(path);
        }

        public static IEnumerable<Mugshot> GetMugshots(NpcConfiguration npc)
        {
            if (npc == null)
                yield break;
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var mugshotModDirs = Directory.GetDirectories(BundlerSettings.Default.MugshotsDirectory);
            var overridingPluginNames = new HashSet<string>(
                npc.Overrides.Select(x => x.PluginName) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            foreach (var mugshotModDir in mugshotModDirs)
            {
                var fileName =
                    Path.Combine(mugshotModDir, npc.BasePluginName, $"00{npc.LocalFormIdHex}.png");
                if (File.Exists(fileName))
                {
                    var modName = Path.GetFileName(Path.TrimEndingDirectorySeparator(mugshotModDir));
                    var matchingPluginNames =
                        modPluginMap.GetPluginsForMod(modName).Where(f => overridingPluginNames.Contains(f)).ToArray();
                    yield return new Mugshot(
                        modName,
                        modPluginMap.IsModInstalled(modName),
                        matchingPluginNames,
                        fileName);
                }
            }
        }

        public bool IsHighlighted { get; set; }
        public bool IsModMissing => !IsModInstalled;
        public bool IsPluginLoaded => ProvidingPlugins.Length > 0;
        public bool IsPluginMissing => ProvidingPlugins.Length == 0;
        public bool IsSelectedSource { get; set; }
    }

    public class NpcConfiguration : INotifyPropertyChanged
    {
        private static readonly HashSet<string> DlcPluginNames = new HashSet<string>(
            new[] { "Update.esm", "Dawnguard.esm", "Dragonborn.esm", "HearthFires.esm" },
            StringComparer.OrdinalIgnoreCase);

        public event Action FaceModChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string FaceModName { get; private set; }
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public IReadOnlyList<NpcOverrideConfiguration> Overrides { get; init; }
        public string Name => npc.Name;

        private readonly Npc npc;

        private NpcOverrideConfiguration defaultConfig;
        private NpcOverrideConfiguration faceConfig;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            Overrides = GetOverrides().ToList().AsReadOnly();

            var defaultOverride = Overrides.LastOrDefault();
            SetDefaultPlugin(defaultOverride);
            SetFacePlugin(defaultOverride, true);
        }        

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
        }

        public void SetDefaultPlugin(NpcOverrideConfiguration overrideConfig)
        {
            if (defaultConfig != null)
                defaultConfig.IsDefaultSource = false;
            if (overrideConfig != null)
                overrideConfig.IsDefaultSource = true;
            defaultConfig = overrideConfig;
        }

        public void SetFaceMod(string modName, bool detectPlugin)
        {
            FaceModName = modName;
            FaceModChanged?.Invoke();
            if (!detectPlugin || string.IsNullOrEmpty(FaceModName))
                return;
            // It should be rare for the same mugshot to correspond to two plugins *with that NPC* in the load order.
            // A single mod might provide several optional add-on plugins that all modify different NPCs (or do totally
            // different things altogether). If this really does happen, the most logical thing to do is to pick the
            // last plugin in the load order which belongs to that mod, which we can assume is the one responsible for
            // any conflict resolution between that mod/plugin and any other ones.
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var modPlugins = new HashSet<string>(
                modPluginMap.GetPluginsForMod(modName), StringComparer.OrdinalIgnoreCase);
            var lastMatchingPlugin = Overrides
                .Where(x => modPlugins.Contains(x.PluginName))
                .LastOrDefault();
            if (lastMatchingPlugin != null)
                SetFacePlugin(lastMatchingPlugin, false);
        }

        public void SetFacePlugin(NpcOverrideConfiguration defaultConfig, bool detectFaceMod)
        {
            if (faceConfig != null)
                faceConfig.IsFaceSource = false;
            if (defaultConfig != null)
                defaultConfig.IsFaceSource = true;
            faceConfig = defaultConfig;
            if (!detectFaceMod || defaultConfig == null)
                return;
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var lastMatchingModName = modPluginMap
                .GetModsForPlugin(defaultConfig.PluginName)
                .Where(f => Mugshot.Exists(f, BasePluginName, LocalFormIdHex))
                .LastOrDefault();
            if (!string.IsNullOrEmpty(lastMatchingModName))
                SetFaceMod(lastMatchingModName, false);
        }

        private IEnumerable<NpcOverrideConfiguration> GetOverrides()
        {
            // The base plugin is always a valid source for any kind of data, so we need to include that in the list.
            var sources = npc.Overrides.Select(x => new { x.PluginName, x.HasFaceOverride })
                .Prepend(new { PluginName = BasePluginName, HasFaceOverride = true });
            return sources.Select(x => new NpcOverrideConfiguration(this, x.PluginName, x.HasFaceOverride));
        }
    }

    public class NpcOverrideConfiguration : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasFaceOverride { get; init; }
        public bool IsDefaultSource { get; set; }
        public bool IsFaceSource { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsSelected { get; set; }
        public string PluginName { get; init; }

        private readonly NpcConfiguration parentConfig;

        public NpcOverrideConfiguration(NpcConfiguration parentConfig, string pluginName, bool hasFaceOverride)
        {
            this.parentConfig = parentConfig;
            PluginName = pluginName;
            HasFaceOverride = hasFaceOverride;
        }

        public void SetAsDefault()
        {
            parentConfig.SetDefaultPlugin(this);
        }

        public void SetAsFace(bool detectMod = false)
        {
            parentConfig.SetFacePlugin(this, detectMod);
        }
    }
}

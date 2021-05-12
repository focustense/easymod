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
        public Mugshot SelectedMugshot { get; private set; }
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
            ClearNpcHighlights();
            SelectedNpc = npc;
            SelectedNpcOverrides = npc.Overrides;
            Mugshots = GetMugshots().ToList().AsReadOnly();
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

        private void ClearNpcHighlights()
        {
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration>())
                overrideConfig.IsSelected = overrideConfig.IsHighlighted = false;
        }

        private IEnumerable<Mugshot> GetMugshots()
        {
            if (SelectedNpc == null)
                yield break;
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var mugshotModDirs = Directory.GetDirectories(BundlerSettings.Default.MugshotsDirectory);
            var overridingPluginNames = new HashSet<string>(
                SelectedNpcOverrides?.Select(x => x.PluginName) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            foreach (var mugshotModDir in mugshotModDirs)
            {
                var fileName =
                    Path.Combine(mugshotModDir, SelectedNpc.BasePluginName, $"00{SelectedNpc.LocalFormIdHex}.png");
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
    }

    public record Mugshot(
        string ProvidingMod, bool IsModInstalled, string[] ProvidingPlugins, string FileName)
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsHighlighted { get; set; }
        public bool IsModMissing => !IsModInstalled;
        public bool IsPluginLoaded => ProvidingPlugins.Length > 0;
        public bool IsPluginMissing => ProvidingPlugins.Length == 0;
    }

    public class NpcConfiguration : INotifyPropertyChanged
    {
        private static readonly HashSet<string> DlcPluginNames = new HashSet<string>(
            new[] { "Update.esm", "Dawnguard.esm", "Dragonborn.esm", "HearthFires.esm" },
            StringComparer.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
        public string DefaultPluginName { get; set; }
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string FacePluginName { get; set; }
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public IReadOnlyList<NpcOverrideConfiguration> Overrides { get; init; }
        public string Name => npc.Name;

        private readonly Npc npc;

        private NpcOverrideConfiguration defaultSource;
        private NpcOverrideConfiguration faceSource;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            Overrides = GetOverrides().ToList().AsReadOnly();

            var defaultOverride = Overrides.LastOrDefault();
            SetDefaultSource(defaultOverride);
            SetFaceSource(defaultOverride);
        }        

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
        }

        public void SetDefaultSource(NpcOverrideConfiguration overrideConfig)
        {
            if (defaultSource != null)
                defaultSource.IsDefaultSource = false;
            if (overrideConfig != null)
                overrideConfig.IsDefaultSource = true;
            defaultSource = overrideConfig;
        }

        public void SetFaceSource(NpcOverrideConfiguration overrideConfig)
        {
            if (faceSource != null)
                faceSource.IsFaceSource = false;
            if (overrideConfig != null)
                overrideConfig.IsFaceSource = true;
            faceSource = overrideConfig;
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

        public void SetDefaultSource()
        {
            parentConfig.SetDefaultSource(this);
        }

        public void SetFaceSource()
        {
            parentConfig.SetFaceSource(this);
        }
    }
}

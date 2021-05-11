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
        public NpcConfiguration SelectedNpc { get; private set; }
        public bool ShowDlcOverrides { get; set; }
        public bool ShowSinglePluginOverrides { get; set; } = true;

        private readonly SortedDictionary<uint, NpcConfiguration> npcConfigurations = new();

        public ProfileViewModel(IEnumerable<Npc> npcs)
        {
            var npcsWithOverrides = npcs.Where(npc => npc.Overrides.Count > 0);
            foreach (var npc in npcsWithOverrides)
                npcConfigurations.Add(npc.FormId, new NpcConfiguration(npc));
        }

        public void SelectNpc(NpcConfiguration npc)
        {
            SelectedNpc = npc;
            Mugshots = GetMugshots().ToList().AsReadOnly();
        }

        private IEnumerable<Mugshot> GetMugshots()
        {
            if (SelectedNpc == null)
                yield break;
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var mugshotModDirs = Directory.GetDirectories(BundlerSettings.Default.MugshotsDirectory);
            var overridingPluginNames = new HashSet<string>(
                SelectedNpc.Overrides.Select(x => x.PluginName),
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
    {
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
        public string Name => npc.Name;
        [DependsOn("FacePluginName", "MainPluginName")]
        public IEnumerable<NpcOverrideConfiguration> Overrides => GetOverrides();

        private readonly Npc npc;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            FacePluginName = DefaultPluginName = npc.Overrides.LastOrDefault()?.PluginName ?? BasePluginName;
        }

        public IEnumerable<NpcOverrideConfiguration> GetOverrides()
        {
            // The base plugin is always a valid source for any kind of data, so we need to include that in the list.
            var sources = npc.Overrides.Select(x => new { x.PluginName, x.HasFaceOverride })
                .Prepend(new { PluginName = BasePluginName, HasFaceOverride = true });
            return sources.Select(x => new NpcOverrideConfiguration(
                x.PluginName, x.HasFaceOverride, x.PluginName == DefaultPluginName, x.PluginName == FacePluginName,
                () => { DefaultPluginName = x.PluginName; }, () => { FacePluginName = x.PluginName; }));
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
        }
    }

    public class NpcOverrideConfiguration
    {
        public bool HasFaceOverride { get; init; }
        public bool IsDefaultSource { get; init; }
        public bool IsFaceSource { get; init; }
        public string PluginName { get; init; }
        public Action SetDefaultSource { get; init; }
        public Action SetFaceSource { get; init; }

        public NpcOverrideConfiguration(
            string pluginName, bool hasFaceOverride, bool isDefaultSource, bool isFaceSource, Action setDefaultSource,
            Action setFaceSource)
        {
            PluginName = pluginName;
            HasFaceOverride = hasFaceOverride;
            IsDefaultSource = isDefaultSource;
            IsFaceSource = isFaceSource;
            SetDefaultSource = setDefaultSource;
            SetFaceSource = setFaceSource;
        }
    }
}

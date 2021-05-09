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
                return NpcConfigurations
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

        protected List<NpcConfiguration> NpcConfigurations { get; init; }

        public ProfileViewModel(IEnumerable<Npc> npcs)
        {
            NpcConfigurations = npcs
                .Where(npc => npc.Overrides.Count > 0)
                .Select(npc => new NpcConfiguration(npc))
                .ToList();
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
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public string Name => npc.Name;
        public string OverridePluginName { get; set; }
        public IReadOnlyList<NpcOverride> Overrides => npc.Overrides;

        private readonly Npc npc;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            OverridePluginName = npc.Overrides.LastOrDefault().PluginName;
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
        }
    }
}

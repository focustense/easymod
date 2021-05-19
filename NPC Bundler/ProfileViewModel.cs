using Ookii.Dialogs.Wpf;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public ProfileViewModel(IEnumerable<Npc> npcs, IEnumerable<string> masterNames)
        {
            var npcsWithOverrides = npcs.Where(npc => npc.Overrides.Count > 0);
            var masterNameSet = new HashSet<string>(masterNames);
            foreach (var npc in npcsWithOverrides)
                npcConfigurations.Add(npc.FormId, new NpcConfiguration(npc, masterNameSet));
        }

        public IEnumerable<NpcConfiguration> GetAllNpcConfigurations()
        {
            return npcConfigurations.Values;
        }

        public void LoadFromFile(Window dialogOwner)
        {
            var dialog = new VistaOpenFileDialog
            {
                Title = "Choose saved profile",
                CheckFileExists = true,
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = false
            };
            var result = dialog.ShowDialog(dialogOwner).GetValueOrDefault();
            if (!result)
                return;
            var savedProfile = SavedProfile.LoadFromFile(dialog.FileName);
            var npcMatches = savedProfile.Npcs
                .Join(npcConfigurations.Values,
                    x => Tuple.Create(x.BasePluginName.ToLowerInvariant(), x.LocalFormIdHex),
                    y => Tuple.Create(y.BasePluginName.ToLowerInvariant(), y.LocalFormIdHex),
                    (x, y) => new { Saved = x, Current = y });
            foreach (var match in npcMatches)
            {
                if (!string.IsNullOrEmpty(match.Saved.DefaultPluginName))
                    match.Current.SetDefaultPlugin(match.Saved.DefaultPluginName);
                if (!string.IsNullOrEmpty(match.Saved.FacePluginName))
                    match.Current.SetFacePlugin(match.Saved.FacePluginName, true);
                if (!string.IsNullOrEmpty(match.Saved.FaceModName))
                    match.Current.SetFaceMod(match.Saved.FaceModName, false);
            }
        }

        public void SaveToFile(Window dialogOwner)
        {
            var dialog = new VistaSaveFileDialog
            {
                Title = "Choose where to save this profile",
                CheckPathExists = true,
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt",
                OverwritePrompt = true
            };
            var result = dialog.ShowDialog(dialogOwner).GetValueOrDefault();
            if (!result)
                return;
            var savedNpcs = npcConfigurations.Values
                .Select(x => new SavedNpcConfiguration
                {
                    BasePluginName = x.BasePluginName,
                    LocalFormIdHex = x.LocalFormIdHex,
                    DefaultPluginName = x.DefaultPluginName,
                    FacePluginName = x.FacePluginName,
                    FaceModName = x.FaceModName,
                })
                .ToList();
            var savedProfile = new SavedProfile { Npcs = savedNpcs };
            savedProfile.SaveToFile(dialog.FileName);
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

        [SuppressPropertyChangedWarnings]
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
            // TODO: Provide placeholder items for mods that provide the facegen but don't have mugshots, so that users
            // can select them anyway. Also provide placeholder for vanilla/default so that we can unapply a mod.
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
}

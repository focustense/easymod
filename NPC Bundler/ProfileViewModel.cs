using Ookii.Dialogs.Wpf;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NPC_Bundler
{
    public class ProfileViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ColumnDefinitions Columns { get; private init; }

        [DependsOn(
            "NpcConfigurations", "OnlyFaceOverrides", "ShowDlcOverrides", "ShowSinglePluginOverrides",
            "DisplayedNpcsSentinel")]
        public IEnumerable<NpcConfiguration<TKey>> DisplayedNpcs
        {
            get {
                var minOverrideCount = ShowSinglePluginOverrides ? 1 : 2;
                var displayedNpcs = GetAllNpcConfigurations();
                ApplyFilter(ref displayedNpcs, Columns.BasePluginName, x => x.BasePluginName);
                ApplyFilter(ref displayedNpcs, Columns.LocalFormIdHex, x => x.LocalFormIdHex);
                ApplyFilter(ref displayedNpcs, Columns.EditorId, x => x.EditorId);
                ApplyFilter(ref displayedNpcs, Columns.Name, x => x.Name);
                return displayedNpcs
                    .Where(x => x.GetOverrideCount(ShowDlcOverrides, !OnlyFaceOverrides) >= minOverrideCount);
            }
        }

        public Mugshot FocusedMugshot { get; set; }
        public NpcOverrideConfiguration<TKey> FocusedNpcOverride { get; set; }
        [DependsOn("SelectedNpc")]
        public bool HasSelectedNpc => SelectedNpc != null;
        public IReadOnlyList<Mugshot> Mugshots { get; private set; }
        public bool OnlyFaceOverrides { get; set; }
        public NpcOverrideConfiguration<TKey> SelectedOverrideConfig { get; private set; }
        public NpcConfiguration<TKey> SelectedNpc { get; set; }
        public IReadOnlyList<NpcOverrideConfiguration<TKey>> SelectedNpcOverrides { get; private set; }
        public bool ShowDlcOverrides { get; set; }
        public bool ShowSinglePluginOverrides { get; set; } = true;

        // PropertyChanged.Fody doesn't have supported for "nested" properties, so this is a convenient way for an
        // internal handler to force the DisplayedNpcs to update (just invert the value).
        protected bool DisplayedNpcsSentinel { get; private set; }

        private readonly ProfileEventLog profileEventLog;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly Dictionary<TKey, NpcConfiguration<TKey>> npcConfigurations = new();
        private readonly IReadOnlyList<TKey> npcOrder;

        public ProfileViewModel(
            IEnumerable<INpc<TKey>> npcs, IModPluginMapFactory modPluginMapFactory, IEnumerable<string> masterNames,
            ProfileEventLog profileEventLog)
        {
            this.modPluginMapFactory = modPluginMapFactory;
            var npcsWithOverrides = npcs.Where(npc => npc.Overrides.Count > 0);
            var masterNameSet = new HashSet<string>(masterNames);

            var npcOrder = new List<TKey>();
            foreach (var npc in npcsWithOverrides)
            {
                npcOrder.Add(npc.Key);
                var npcConfig = new NpcConfiguration<TKey>(npc, modPluginMapFactory, masterNameSet);
                npcConfigurations.Add(npc.Key, npcConfig);
            }
            this.npcOrder = npcOrder.AsReadOnly();

            this.profileEventLog = profileEventLog;
            var restored = RestoreProfileAutosave(profileEventLog.FileName).ToLookup(x => x.Item1.Key, x => x.Item2);
            var allProfileFields = Enum.GetValues<NpcProfileField>();
            foreach (var npcConfig in npcConfigurations.Values)
            {
                // This event handler is added but never removed or disposed - because NPC configurations have the same
                // lifetime as the app itself. The entries themselves are mutable, but the list of NPCs can't change and
                // is never reloaded.
                npcConfig.ProfilePropertyChanged += OnNpcProfilePropertyChanged;

                // With the event handler now hooked up, we need to simulate "new" events for any fields that aren't in
                // the autosave, so that they won't be subject to load-order changes on subsequent starts.
                var newFields = allProfileFields.Except(restored[npcConfig.Key]);
                npcConfig.EmitProfileEvents(newFields);
            }

            Columns = new()
            {
                BasePluginName = RegisterNpcGridColumn("Base Plugin"),
                EditorId = RegisterNpcGridColumn("Editor ID"),
                LocalFormIdHex = RegisterNpcGridColumn("Form ID"),
                Name = RegisterNpcGridColumn("Name"),
            };
        }

        private IEnumerable<Tuple<NpcConfiguration<TKey>, NpcProfileField>> RestoreProfileAutosave(
            string eventLogFileName)
        {
            // Mutagen keys can be derived directly from the master plugin name and we don't really need the extra
            // dictionary for it, but XEdit doesn't, so until it's removed, this has to be explicit.
            var npcConfigurationsByPluginKey = npcConfigurations.Values
                .ToDictionary(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex));
            var restoredProfileEvents = ProfileEventLog.ReadEventsFromFile(eventLogFileName);
            foreach (var e in restoredProfileEvents)
            {
                var npcKey = Tuple.Create(e.BasePluginName, e.LocalFormIdHex);
                if (npcConfigurationsByPluginKey.TryGetValue(npcKey, out var npcConfig))
                {
                    npcConfig.RestoreFromProfileEvent(e);
                    yield return Tuple.Create(npcConfig, e.Field);
                }
            }
        }

        public IEnumerable<NpcConfiguration<TKey>> GetAllNpcConfigurations()
        {
            return npcOrder.Select(key => npcConfigurations[key]);
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
            var savedNpcs = GetAllNpcConfigurations()
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

        public void SetFaceOverride(Mugshot mugshot, bool detectPlugin = false)
        {
            SelectedNpc?.SetFaceMod(mugshot?.ProvidingMod, detectPlugin);
            SyncMugshotMod();
        }

        protected void OnFocusedMugshotChanged()
        {
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration<TKey>>())
                overrideConfig.IsHighlighted =
                    FocusedMugshot?.ProvidingPlugins?.Contains(
                        overrideConfig.PluginName, StringComparer.OrdinalIgnoreCase) ??
                    false;
            if (FocusedMugshot != null)
            {
                foreach (var ms in Mugshots ?? Enumerable.Empty<Mugshot>())
                    ms.IsHighlighted = false;
                FocusedNpcOverride = null;
            }
        }

        protected void OnFocusedNpcOverrideChanged()
        {
            ClearNpcSelections();
            foreach (var mugshot in Mugshots ?? Enumerable.Empty<Mugshot>())
                mugshot.IsHighlighted =
                    mugshot.ProvidingPlugins.Contains(FocusedNpcOverride?.PluginName, StringComparer.OrdinalIgnoreCase);
            if (FocusedNpcOverride != null)
            {
                ClearNpcHighlights();
                FocusedNpcOverride.IsSelected = true;
                FocusedMugshot = null;
            }
        }

        protected void OnSelectedNpcChanged(object before, object after)
        {
            var next = after as NpcConfiguration<TKey>;
            if (before is NpcConfiguration<TKey> previous)
                previous.FaceModChanged -= OnNpcFaceModChanged;
            ClearNpcHighlights();
            ClearNpcSelections();
            SelectedNpcOverrides = next?.Overrides;
            Mugshots = Mugshot.GetMugshots(next, modPluginMapFactory.DefaultMap())
                .OrderBy(x => x.ProvidingMod)
                .ToList()
                .AsReadOnly();
            SyncMugshotMod();
            FocusedMugshot = null;
            if (next != null)
                next.FaceModChanged += OnNpcFaceModChanged;
        }

        private static void ApplyFilter(
            ref IEnumerable<NpcConfiguration<TKey>> npcs, DataGridColumn column,
            Func<NpcConfiguration<TKey>, string> propertySelector)
        {
            if (string.IsNullOrEmpty(column?.FilterText))
                return;
            npcs = npcs.Where(x =>
                propertySelector(x)?.Contains(column.FilterText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private void ClearNpcHighlights()
        {
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration<TKey>>())
                overrideConfig.IsHighlighted = false;
        }

        private void ClearNpcSelections()
        {
            foreach (var overrideConfig in SelectedNpcOverrides ?? Enumerable.Empty<NpcOverrideConfiguration<TKey>>())
                overrideConfig.IsSelected = false;
        }

        [SuppressPropertyChangedWarnings]
        private void OnNpcFaceModChanged()
        {
            SyncMugshotMod();
        }

        [SuppressPropertyChangedWarnings]
        private void OnNpcProfilePropertyChanged(object sender, ProfileEvent e)
        {
            profileEventLog.Append(e);
        }

        private DataGridColumn RegisterNpcGridColumn(string headerText)
        {
            var column = new DataGridColumn(headerText);
            column.PropertyChanged += (_, _) => UpdateNpcColumnFilters();
            return column;
        }

        private void SyncMugshotMod()
        {
            foreach (var ms in Mugshots)
                ms.IsSelectedSource =
                    ms.ProvidingMod == SelectedNpc?.FaceModName ||
                    (string.IsNullOrEmpty(ms.ProvidingMod) && string.IsNullOrEmpty(SelectedNpc?.FaceModName));
        }

        private void UpdateNpcColumnFilters()
        {
            DisplayedNpcsSentinel = !DisplayedNpcsSentinel;
        }

        public class ColumnDefinitions
        {
            public DataGridColumn BasePluginName { get; init; }
            public DataGridColumn LocalFormIdHex { get; init; }
            public DataGridColumn EditorId { get; init; }
            public DataGridColumn Name { get; init; }
        }
    }

    public class Mugshot : INotifyPropertyChanged
    {
        private static readonly string AssetsDirectory = Path.Combine(
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
            "assets");

        public event PropertyChangedEventHandler PropertyChanged;

        public static bool Exists(string modName, string basePluginName, string localFormIdHex)
        {
            var mugshotsDirectory = !string.IsNullOrEmpty(BundlerSettings.Default.MugshotsDirectory) ?
                BundlerSettings.Default.MugshotsDirectory : ProgramData.DefaultMugshotsPath;
            var path = Path.Combine(mugshotsDirectory, modName, basePluginName, $"00{localFormIdHex}.png");
            return File.Exists(path);
        }

        public static IEnumerable<Mugshot> GetMugshots<TKey>(NpcConfiguration<TKey> npc, ModPluginMap modPluginMap)
            where TKey : struct
        {
            if (npc == null)
                yield break;
            // TODO: Provide placeholder items for mods that provide the facegen but don't have mugshots, so that users
            // can select them anyway. Also provide placeholder for vanilla/default so that we can unapply a mod.
            var mugshotModDirs = Directory.GetDirectories(BundlerSettings.Default.MugshotsDirectory);
            var overridingPluginNames = new HashSet<string>(
                npc.Overrides.Select(x => x.PluginName) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var handledModNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        modName, modPluginMap.IsModInstalled(modName), matchingPluginNames, fileName, false);
                    handledModNames.Add(modName);
                }
            }
            foreach (var pluginName in npc.Overrides.Select(x => x.PluginName))
            {
                var modNames = modPluginMap.GetModsForPlugin(pluginName)
                    .Where(modName => !handledModNames.Contains(modName));
                var isVanillaOrDlc = FileStructure.IsVanilla(pluginName) || FileStructure.IsDlc(pluginName);
                if (isVanillaOrDlc && !modNames.Any())
                    modNames = modNames.Concat(new[] { "" });
                foreach (var modName in modNames.Distinct())
                {
                    var filePrefix = npc.IsFemale ? "female" : "male";
                    var fileName = Path.Combine(AssetsDirectory, $"{filePrefix}-silhouette.png");
                    yield return new Mugshot(
                            modName, isVanillaOrDlc || modPluginMap.IsModInstalled(modName), new[] { pluginName },
                            fileName, true);
                }
            }
        }

        public Mugshot(string providingMod, bool isModInstalled, string[] providingPlugins, string fileName, bool isGeneric)
        {
            ProvidingMod = providingMod;
            IsModInstalled = isModInstalled;
            ProvidingPlugins = providingPlugins;
            FileName = fileName;
            IsGeneric = isGeneric;
        }

        public bool IsGeneric { get; init; }
        public bool IsHighlighted { get; set; }
        public bool IsModInstalled { get; init; }
        public bool IsModMissing => !IsModInstalled;
        public bool IsPluginLoaded => ProvidingPlugins.Length > 0;
        public bool IsPluginMissing => ProvidingPlugins.Length == 0;
        public bool IsSelectedSource { get; set; }
        public string FileName { get; init; }
        public string ModDisplayName => !string.IsNullOrEmpty(ProvidingMod) ? ProvidingMod : "Default (Vanilla)";
        public string ProvidingMod { get; init; }
        public string[] ProvidingPlugins { get; init; }
    }
}

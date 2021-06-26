using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
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

namespace Focus.Apps.EasyNpc.Profile
{
    public class ProfileViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ColumnDefinitions Columns { get; private init; }

        [DependsOn("NpcConfigurations", "OnlyFaceOverrides", "ShowSinglePluginOverrides", "DisplayedNpcsSentinel")]
        public IEnumerable<NpcConfiguration<TKey>> DisplayedNpcs
        {
            get { return ApplyFilters(GetAllNpcConfigurations()); }
        }

        public NpcFilters Filters { get; private init; } = new NpcFilters();
        public Mugshot FocusedMugshot { get; set; }
        public NpcOverrideConfiguration<TKey> FocusedNpcOverride { get; set; }
        [DependsOn("SelectedNpc")]
        public bool HasSelectedNpc => SelectedNpc != null;
        public IReadOnlyList<string> LoadedPluginNames { get; private init; }
        public IReadOnlyList<Mugshot> Mugshots { get; private set; }
        public bool OnlyFaceOverrides { get; set; }
        public NpcOverrideConfiguration<TKey> SelectedOverrideConfig { get; private set; }
        public NpcConfiguration<TKey> SelectedNpc { get; set; }
        public IReadOnlyList<NpcOverrideConfiguration<TKey>> SelectedNpcOverrides { get; private set; }
        public bool ShowSinglePluginOverrides { get; set; } = true;

        // PropertyChanged.Fody doesn't have supported for "nested" properties, so this is a convenient way for an
        // internal handler to force the DisplayedNpcs to update (just invert the value).
        protected bool DisplayedNpcsSentinel { get; private set; }

        private readonly IReadOnlySet<string> loadedPluginNamesSet;
        private readonly ProfileEventLog profileEventLog;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly Dictionary<TKey, NpcConfiguration<TKey>> npcConfigurations = new();
        private readonly IReadOnlyList<TKey> npcOrder;

        public ProfileViewModel(
            IEnumerable<INpc<TKey>> npcs, IModPluginMapFactory modPluginMapFactory,
            IEnumerable<string> loadedPluginNames, IEnumerable<string> masterNames, ProfileEventLog profileEventLog)
        {
            this.modPluginMapFactory = modPluginMapFactory;
            LoadedPluginNames = loadedPluginNames.OrderBy(x => x).ToList().AsReadOnly();
            loadedPluginNamesSet = LoadedPluginNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var npcsWithOverrides = npcs.Where(npc => npc.Overrides.Count > 0);
            var profileRuleSet = StandardProfileRuleSet.Create(masterNames, npcs);

            var npcOrder = new List<TKey>();
            foreach (var npc in npcsWithOverrides)
            {
                npcOrder.Add(npc.Key);
                var npcConfig = new NpcConfiguration<TKey>(npc, modPluginMapFactory, profileRuleSet);
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

            Filters.PropertyChanged += (_, _) => RefreshDisplayedNpcs();
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

        private static void ApplyColumnFilter(
            ref IEnumerable<NpcConfiguration<TKey>> npcs, DataGridColumn column,
            Func<NpcConfiguration<TKey>, string> propertySelector)
        {
            if (string.IsNullOrEmpty(column?.FilterText))
                return;
            npcs = npcs.Where(x =>
                propertySelector(x)?.Contains(column.FilterText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private IEnumerable<NpcConfiguration<TKey>> ApplyFilters(IEnumerable<NpcConfiguration<TKey>> npcs)
        {
            var displayedNpcs = npcs;
            var minOverrideCount = ShowSinglePluginOverrides ? 1 : 2;
            ApplyColumnFilter(ref displayedNpcs, Columns.BasePluginName, x => x.BasePluginName);
            ApplyColumnFilter(ref displayedNpcs, Columns.LocalFormIdHex, x => x.LocalFormIdHex);
            ApplyColumnFilter(ref displayedNpcs, Columns.EditorId, x => x.EditorId);
            ApplyColumnFilter(ref displayedNpcs, Columns.Name, x => x.Name);
            var modPluginMap = modPluginMapFactory.DefaultMap();
            if (Filters.Wigs)
                displayedNpcs = displayedNpcs.Where(x => x.FaceConfiguration?.HasWig == true);
            if (!string.IsNullOrEmpty(Filters.DefaultPlugin))
                displayedNpcs = displayedNpcs.Where(x =>
                    x.DefaultPluginName.Equals(Filters.DefaultPlugin, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(Filters.FacePlugin))
                displayedNpcs = displayedNpcs.Where(x =>
                    x.FacePluginName.Equals(Filters.FacePlugin, StringComparison.OrdinalIgnoreCase));
            if (Filters.Conflicts)
                displayedNpcs = displayedNpcs.Where(x =>
                    (x.HasFaceCustomizations() || !string.IsNullOrEmpty(x.FaceModName)) &&
                    !modPluginMap
                        .GetModsForPlugin(x.FacePluginName)
                        .Contains(x.FaceModName, StringComparer.OrdinalIgnoreCase));
            if (Filters.Missing)
            {
                // The loading process will have selected alternate plugins, so we have to go back to the autosave to
                // find out where they "should" be pointing.
                //
                // TODO: This makes it possible to find them, but what about visualizing them? How can the user actually
                // see what the problem is, after selecting one of these NPCS?
                // This condition is starting to be recognized in a few different places now, and there needs to be a
                // better way to handle it, both in terms of efficiency and usability.
                var npcsWithMissingPlugins = profileEventLog
                    .MostRecentByNpc()
                    .WithMissingPlugins(loadedPluginNamesSet, modPluginMap)
                    .Select(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex))
                    .ToHashSet();
                displayedNpcs = displayedNpcs.Where(x =>
                    npcsWithMissingPlugins.Contains(Tuple.Create(x.BasePluginName, x.LocalFormIdHex)) ||
                    (!string.IsNullOrEmpty(x.FaceModName) && !modPluginMap.IsModInstalled(x.FaceModName)));
            }
            return displayedNpcs
                .Where(x => x.GetOverrideCount(!Filters.NonDlc, !OnlyFaceOverrides) >= minOverrideCount);
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

        private void RefreshDisplayedNpcs()
        {
            DisplayedNpcsSentinel = !DisplayedNpcsSentinel;
        }

        private DataGridColumn RegisterNpcGridColumn(string headerText)
        {
            var column = new DataGridColumn(headerText);
            column.PropertyChanged += (_, _) => RefreshDisplayedNpcs();
            return column;
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

        private void SyncMugshotMod()
        {
            foreach (var ms in Mugshots)
                ms.IsSelectedSource =
                    ms.ProvidingMod == SelectedNpc?.FaceModName ||
                    (string.IsNullOrEmpty(ms.ProvidingMod) && string.IsNullOrEmpty(SelectedNpc?.FaceModName));
        }

        public class ColumnDefinitions
        {
            public DataGridColumn BasePluginName { get; init; }
            public DataGridColumn LocalFormIdHex { get; init; }
            public DataGridColumn EditorId { get; init; }
            public DataGridColumn Name { get; init; }
        }

        public class NpcFilters : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public bool Conflicts { get; set; }
            public string DefaultPlugin { get; set; }
            public string FacePlugin { get; set; }
            public bool Missing { get; set; }
            public bool NonDlc { get; set; } = true;
            public bool Wigs { get; set; }
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
            var redirect = GetRedirect(modName);
            var candidatePaths = new[] { modName, redirect }
                .Select(dir => Path.Combine(
                    ProgramData.ConfiguredMugshotsPath, dir, basePluginName, $"00{localFormIdHex}.png"));
            return candidatePaths.Any(File.Exists);
        }

        public static IEnumerable<Mugshot> GetMugshots<TKey>(NpcConfiguration<TKey> npc, ModPluginMap modPluginMap)
            where TKey : struct
        {
            if (npc == null || !Directory.Exists(ProgramData.ConfiguredMugshotsPath))
                yield break;
            // TODO: Provide placeholder items for mods that provide the facegen but don't have mugshots, so that users
            // can select them anyway. Also provide placeholder for vanilla/default so that we can unapply a mod.
            var mugshotModDirs = Directory.GetDirectories(ProgramData.ConfiguredMugshotsPath);
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
                    var mugshotName = Path.GetFileName(Path.TrimEndingDirectorySeparator(mugshotModDir));
                    var candidateMugshots = Settings.Default.MugshotRedirects
                        .Where(x => string.Equals(mugshotName, x.Mugshots, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.ModName)
                        .Prepend(mugshotName)
                        .Select(modName => GetMugshot(fileName, modName, modPluginMap, overridingPluginNames));
                    // The reason for attempting to obtain the "best" mugshot here, as opposed to producing all of the
                    // matches, is so that we can avoid duplicates in the list. If the mugshots are named "My Overhaul",
                    // and the mod is named "My Overhaul 2.0", and we have a redirect from "My Overhaul 2.0" to "My
                    // Overhaul", then it would make no sense and be visually confusing to see the same mugshot appear
                    // twice, one for "My Overhaul" (which is not installed) and one for "My Overhaul 2.0" (which is).
                    //
                    // Another way of describing this is that we try to show, and correctly label, mugshots for mods
                    // that are actually installed. Showing a mugshot for a non-installed mod is just there as a neat
                    // discovery feature for people first getting into overhauls or not sure whether they want to
                    // install the mod. But it shouldn't interfere with users who already know what they want and have
                    // already set up the redirects correctly.
                    //
                    // In theory this could exclude some mugshots if the redirects are set up really badly, e.g. if Mod
                    // X is set up to redirect to mugshots for totally unrelated Mod Y, and both are installed. At some
                    // point we just have to trust that typical users won't mess things up that badly, and if they do,
                    // will think to look at their settings again when trying to fix it.
                    var bestMugshot = GetBestMugshot(candidateMugshots);
                    yield return bestMugshot;
                    if (bestMugshot != null && bestMugshot.IsModInstalled)
                        handledModNames.Add(bestMugshot.ProvidingMod);
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
                    var fileName = GetGenericMugshotImage(npc);
                    yield return new Mugshot(
                            modName, isVanillaOrDlc || modPluginMap.IsModInstalled(modName), new[] { pluginName },
                            fileName, true);
                    handledModNames.Add(modName);
                }
            }
            // It's possible that an NPC is configured with a face mod that is no longer "reachable" through any of the
            // installed plugins. We still want to show this as a mugshot, for that specific NPC only, so that the user
            // can tell what's actually going on.
            if (!string.IsNullOrEmpty(npc.FaceModName) && !handledModNames.Contains(npc.FaceModName))
                yield return new Mugshot(
                    npc.FaceModName, modPluginMap.IsModInstalled(npc.FaceModName), Array.Empty<string>(),
                    GetGenericMugshotImage(npc), true);
        }

        private static Mugshot GetBestMugshot(IEnumerable<Mugshot> mugshots)
        {
            // This could be written more elegantly with LINQ, but performance is potentially tight here if the user is
            // going to be scrolling down a long list very quickly. This way guarantees that the list is iterated at
            // most once, and possibly short-circuited in a best-case scenario.
            //
            // Effectively implements the following priorities:
            // 1. Mod was detected AND has a plugin in the load order (i.e. we're definitely using it)
            // 2. Mod was NOT detected but plugin is still in the load order (probably has missing/incorrect redirect)
            // 3. No plugin, but mod was found (might be disabled, merged, etc.)
            // 4. No evidence whatsoever of mod being installed (i.e. have mugshot without the mod)
            Mugshot bestMugshot = null;
            foreach (var mugshot in mugshots)
            {
                if (mugshot.IsModInstalled && mugshot.IsPluginLoaded)
                    return mugshot;
                if (bestMugshot == null ||
                    !bestMugshot.IsPluginLoaded && mugshot.IsPluginLoaded ||
                    !bestMugshot.IsPluginLoaded && !bestMugshot.IsModInstalled && mugshot.IsModInstalled)

                    bestMugshot = mugshot;
            }
            return bestMugshot;
        }

        private static string GetGenericMugshotImage<TKey>(NpcConfiguration<TKey> npc)
            where TKey : struct
        {
            var filePrefix = npc.IsFemale ? "female" : "male";
            return Path.Combine(AssetsDirectory, $"{filePrefix}-silhouette.png");
        }

        private static Mugshot GetMugshot(
            string fileName, string modName, ModPluginMap modPluginMap, IReadOnlySet<string> pluginNames)
        {
            var matchingPluginNames =
                modPluginMap.GetPluginsForMod(modName).Where(f => pluginNames.Contains(f)).ToArray();
            return new Mugshot(modName, modPluginMap.IsModInstalled(modName), matchingPluginNames, fileName, false);
        }

        private static string GetRedirect(string modName)
        {
            return Settings.Default.MugshotRedirects
                .FirstOrDefault(x => string.Equals(x.ModName, modName, StringComparison.OrdinalIgnoreCase))
                ?.Mugshots ?? modName;
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

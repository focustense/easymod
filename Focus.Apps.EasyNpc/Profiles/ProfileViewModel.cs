#nullable enable

using Focus.Apps.EasyNpc.GameData.Files;
using Ookii.Dialogs.Wpf;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public NpcFiltersViewModel Filters { get; private init; } = new();
        public NpcGridViewModel Grid { get; private init; }
        [DependsOn("SelectedNpc")]
        public bool HasSelectedNpc => Grid.SelectedNpc is not null;
        public NpcSearchParameters Search { get; private init; } = new();
        public NpcViewModel? SelectedNpc { get; private set; }

        private readonly HashSet<IRecordKey> alwaysVisibleNpcKeys = new(RecordKeyComparer.Default);
        private readonly ILineupBuilder lineupBuilder;
        private readonly Dictionary<IRecordKey, NpcModel> npcs = new(RecordKeyComparer.Default);
        private readonly Dictionary<string, int> pluginOrder;
        private readonly Profile profile;

        public ProfileViewModel(Profile profile, ILineupBuilder lineupBuilder, IGameSettings gameSettings)
        {
            this.lineupBuilder = lineupBuilder;
            this.npcs = profile.Npcs.ToDictionary(x => new RecordKey(x), RecordKeyComparer.Default);
            this.profile = profile;

            pluginOrder = gameSettings.PluginLoadOrder
                .Select((pluginName, index) => (pluginName, index))
                .ToDictionary(x => x.pluginName, x => x.index);

            Grid = new NpcGridViewModel(Search);
            Grid.WhenChanged(nameof(Grid.SelectedNpc), () => UpdateSelectedNpc(Grid.SelectedNpc));
            Filters.AvailablePlugins = gameSettings.PluginLoadOrder.OrderBy(f => f).ToList();

            Filters.PropertyChanged += (_, _) => ApplyFilters();
            Search.PropertyChanged += (_, _) => ApplyFilters();

            ApplyFilters();
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
            if (dialog.ShowDialog(dialogOwner).GetValueOrDefault())
            {
                profile.Load(dialog.FileName);
                ApplyFilters();
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
            if (dialog.ShowDialog(dialogOwner).GetValueOrDefault())
                profile.Save(dialog.FileName);
        }

        public bool SelectNpc(IRecordKey key)
        {
            if (npcs.TryGetValue(key, out var npc))
            {
                Grid.SelectedNpc = npc;
                return true;
            }
            return false;
        }

        private void ApplyFilters()
        {
            var filteredNpcs = npcs.Values.AsEnumerable()
                // We'll add the "always visible" back at the end.
                .Where(x => !alwaysVisibleNpcKeys.Contains(x));
            var minOverrideCount = !Filters.MultipleChoices ? 1 : 2;
            ApplySearchParameter(ref filteredNpcs, x => x.BasePluginName);
            ApplySearchParameter(ref filteredNpcs, x => x.LocalFormIdHex);
            ApplySearchParameter(ref filteredNpcs, x => x.EditorId);
            ApplySearchParameter(ref filteredNpcs, x => x.Name);
            if (Filters.Wigs)
                filteredNpcs = filteredNpcs.Where(x => x.FaceOption.HasWig);
            if (!string.IsNullOrEmpty(Filters.DefaultPlugin))
                filteredNpcs = filteredNpcs.Where(x => x.DefaultOption.PluginEquals(Filters.DefaultPlugin));
            if (!string.IsNullOrEmpty(Filters.FacePlugin))
                filteredNpcs = filteredNpcs.Where(x => x.FaceOption.PluginEquals(Filters.FacePlugin));
            if (Filters.Conflicts)
                // Not really a "conflict" anymore, but we'll repurpose the filter.
                filteredNpcs = filteredNpcs.Where(x => x.FaceGenOverride is not null);
            if (Filters.Missing)
                // TODO: Also check for invalid facegen override plugin?
                filteredNpcs = filteredNpcs.Where(x => x.HasMissingPlugins);
            filteredNpcs = filteredNpcs
                .Where(x => x.GetOverrideCount(!Filters.NonDlc, !Filters.FaceChanges) >= minOverrideCount);
            filteredNpcs = filteredNpcs
                // This is only the default ordering; grid ordering is independent.
                .OrderBy(x => pluginOrder.GetOrDefault(x.BasePluginName))
                .ThenBy(x => uint.TryParse(x.LocalFormIdHex, NumberStyles.HexNumber, null, out var formId) ?
                    formId : uint.MaxValue);
            Grid.Npcs = alwaysVisibleNpcKeys
                .Select(x => npcs.GetOrDefault(x))
                .NotNull()
                .Concat(filteredNpcs);
        }

        private void ApplySearchParameter(
            ref IEnumerable<NpcModel> npcs, Func<INpcSearchParameters, string> propertySelector)
        {
            var filterText = propertySelector(Search);
            if (string.IsNullOrEmpty(filterText))
                return;
            npcs = npcs.Where(x =>
                propertySelector(x)?.Contains(filterText, StringComparison.CurrentCultureIgnoreCase) ?? false);
        }

        private IAsyncEnumerable<MugshotModel> GetMugshots(NpcModel? npc)
        {
            if (npc is null)
                return AsyncEnumerable.Empty<MugshotModel>();
            var affectingPlugins = npc.Options
                .Where(x => !string.Equals(x.PluginName, FileStructure.MergeFileName, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.PluginName);
            return lineupBuilder.Build(npc, affectingPlugins);
        }

        private void UpdateSelectedNpc(NpcModel? npc)
        {
            if (npc is null)
            {
                SelectedNpc = null;
                return;
            }
            var mugshots = GetMugshots(npc);
            SelectedNpc = new NpcViewModel(npc, mugshots);
        }
    }
}

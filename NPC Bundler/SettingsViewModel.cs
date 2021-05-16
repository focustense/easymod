using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NPC_Bundler
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly BundlerSettings Settings = BundlerSettings.Default;

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<string> AvailablePlugins { get; set; }
        public ObservableCollection<BuildWarningSuppressions> BuildWarningWhitelist { get; init; }

        public SettingsViewModel()
        {
            var entries = Settings.BuildWarningWhitelist
                .Cast<string>()
                .Select(s => s.Split('='))
                .Where(items => items.Length == 2)
                .Select(items => new BuildWarningSuppressions(items[0], BuildWarningSuppressions.ParseWarnings(items[1])));
            BuildWarningWhitelist = new(entries);
            BuildWarningWhitelist.CollectionChanged += BuildWarningWhitelist_CollectionChanged;
            foreach (var entry in BuildWarningWhitelist)
                Watch(entry);
        }

        public string ModRootDirectory
        {
            get => Settings.ModRootDirectory;
            set
            {
                Settings.ModRootDirectory = value;
                Settings.Save();
            }
        }

        public string MugshotsDirectory
        {
            get => Settings.MugshotsDirectory;
            set
            {
                Settings.MugshotsDirectory = value;
                Settings.Save();
            }
        }

        public void AddBuildWarningSuppression()
        {
            BuildWarningWhitelist.Add(new(AvailablePlugins.FirstOrDefault()));
        }

        public void RemoveBuildWarningSuppression(BuildWarningSuppressions suppressions)
        {
            BuildWarningWhitelist.Remove(suppressions);
        }

        public void SelectModRootDirectory(Window owner)
        {
            const string description = "Select root directory where your individual mod directories are located";
            if (SelectDirectory(owner, description, out string modRootDirectory))
                ModRootDirectory = modRootDirectory;

        }

        public void SelectMugshotsDirectory(Window owner)
        {
            const string description = "Select directory containing face previews";
            if (SelectDirectory(owner, description, out string mugshotsDirectory))
                MugshotsDirectory = mugshotsDirectory;
        }

        private void BuildWarningSuppressions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveBuildWarningSuppressions();
        }

        private void BuildWarningWhitelist_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.Cast<BuildWarningSuppressions>())
                    Unwatch(item);
            if (e.NewItems != null)
                foreach (var item in e.NewItems.Cast<BuildWarningSuppressions>())
                    Watch(item);
            SaveBuildWarningSuppressions();
        }

        private void SaveBuildWarningSuppressions()
        {
            // As suppressions are currently stored as a single serialized collection, we have to regenerate the whole
            // value each time. This probably won't make much of a difference in practice - there's no reason any user
            // should be adding suppressions for hundreds of plugins.
            while (Settings.BuildWarningWhitelist.Count > BuildWarningWhitelist.Count)
                Settings.BuildWarningWhitelist.RemoveAt(Settings.BuildWarningWhitelist.Count - 1);
            while (Settings.BuildWarningWhitelist.Count < BuildWarningWhitelist.Count)
                Settings.BuildWarningWhitelist.Add(null);
            for (int i = 0; i < BuildWarningWhitelist.Count; i++)
            {
                var suppressions = BuildWarningWhitelist[i];
                Settings.BuildWarningWhitelist[i] = $"{suppressions.PluginName}={suppressions.SerializeWarnings()}";
            }
            Settings.Save();
        }

        private bool SelectDirectory(Window owner, string description, out string selectedPath)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = description;
            dialog.UseDescriptionForTitle = true;
            bool result = dialog.ShowDialog(owner).GetValueOrDefault();
            if (result)
                selectedPath = dialog.SelectedPath;
            else
                selectedPath = string.Empty;
            return result;
        }

        private void Unwatch(BuildWarningSuppressions instance)
        {
            instance.PropertyChanged -= BuildWarningSuppressions_PropertyChanged;
        }

        private void Watch(BuildWarningSuppressions instance)
        {
            instance.PropertyChanged += BuildWarningSuppressions_PropertyChanged;
        }
    }

    public record BuildWarningSelection(BuildWarningId Id, bool IsSelected) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BuildWarningSuppressions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static IEnumerable<BuildWarningId> ParseWarnings(string serializedWarnings)
        {
            return serializedWarnings
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (BuildWarningId)int.Parse(s))
                .ToList()
                .AsReadOnly();
        }

        public static string SerializeWarnings(IEnumerable<BuildWarningId> warnings)
        {
            return string.Join(',', warnings.Select(id => (int)id));
        }

        public string PluginName { get; set; }
        public IEnumerable<BuildWarningId> SelectedWarnings { get; private set; }
        public IReadOnlyList<BuildWarningSelection> WarningSelections { get; init; }

        public BuildWarningSuppressions(string pluginName, IEnumerable<BuildWarningId> warnings = null)
        {
            PluginName = pluginName;
            WarningSelections = Enum.GetValues<BuildWarningId>()
                .Select(id => new BuildWarningSelection(id, warnings?.Contains(id) ?? false))
                .ToList()
                .AsReadOnly();
            foreach (var selection in WarningSelections)
                selection.PropertyChanged += (sender, e) => UpdateSelectedWarnings();
            UpdateSelectedWarnings();
        }

        public string SerializeWarnings()
        {
            return SerializeWarnings(SelectedWarnings);
        }

        private void UpdateSelectedWarnings()
        {
            // This could easily be a computed property - but that doesn't play nice with WPF and Fody.PropertyChanged.
            SelectedWarnings = WarningSelections.Where(x => x.IsSelected).Select(x => x.Id);
        }
    }
}

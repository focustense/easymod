using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Messages;
using Focus.ModManagers;
using Ookii.Dialogs.Wpf;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace Focus.Apps.EasyNpc.Configuration
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? WelcomeAcked;

        public IEnumerable<string> AvailableModNames => modRepository.Select(x => x.Name);
        public IEnumerable<string> AvailableMugshotModNames { get; private set; } = Enumerable.Empty<string>();
        public IEnumerable<string> AvailablePlugins => gameSettings.PluginLoadOrder.OrderBy(p => p);
        public ObservableCollection<BuildWarningSuppressionViewModel> BuildWarningWhitelist { get; init; }
        public bool HasDefaultModRootDirectory { get; private init; }
        [DependsOn(nameof(UseModManagerForModDirectory))]
        public bool HasModManagerModDirectory => !string.IsNullOrEmpty(modManagerConfig.ModsDirectory);
        [DependsOn(nameof(ModRootDirectory))]
        public bool IsModDirectorySpecified => !string.IsNullOrEmpty(ModRootDirectory);
        public bool IsWelcomeScreen { get; set; }
        [DependsOn(nameof(ModRootDirectory))]
        public bool ModDirectoryExists => IsModDirectorySpecified && Directory.Exists(ModRootDirectory);
        public string ModManagerModDirectory => modManagerConfig.ModsDirectory;
        public ObservableCollection<MugshotRedirectViewModel> MugshotRedirects { get; init; }
        public string MugshotsDirectoryPlaceholderText => ProgramData.DefaultMugshotsPath;

        public string ModRootDirectory
        {
            get => settings.DefaultModRootDirectory;
            set
            {
                settings.DefaultModRootDirectory = value;
                settings.Save();
                messageBus.Send(new SettingsChanged(SettingsChanged.SettingKind.DefaultModDirectory));
            }
        }

        public string MugshotsDirectory
        {
            get => settings.MugshotsDirectory;
            set
            {
                settings.MugshotsDirectory = value;
                settings.Save();
                messageBus.Send(new SettingsChanged(SettingsChanged.SettingKind.MugshotDirectory));
            }
        }

        public bool UseModManagerForModDirectory
        {
            get => settings.UseModManagerForModDirectory;
            set
            {
                settings.UseModManagerForModDirectory = value;
                settings.Save();
                messageBus.Send(new SettingsChanged(SettingsChanged.SettingKind.ModDirectorySource));
            }
        }

        private readonly IGameSettings gameSettings;
        private readonly IMessageBus messageBus;
        private readonly IModManagerConfiguration modManagerConfig;
        private readonly IModRepository modRepository;
        private readonly IMutableAppSettings settings;

        public SettingsViewModel(
            IMutableAppSettings settings, IModManagerConfiguration modManagerConfig, IModRepository modRepository,
            IGameSettings gameSettings, IMessageBus messageBus)
        {
            this.gameSettings = gameSettings;
            this.messageBus = messageBus;
            this.modManagerConfig = modManagerConfig;
            this.modRepository = modRepository;
            this.settings = settings;

            HasDefaultModRootDirectory = !string.IsNullOrEmpty(ModRootDirectory);

            // Since these are pass-through properties, we need to force an initial update.
            OnMugshotsDirectoryChanged();

            var buildWarningSuppressions = settings.BuildWarningWhitelist
                .Select(x => new BuildWarningSuppressionViewModel(x.PluginName, x.IgnoredWarnings));
            BuildWarningWhitelist = new(buildWarningSuppressions);
            WatchCollection(BuildWarningWhitelist, SaveBuildWarningSuppressions);

            var mugshotRedirects = settings.MugshotRedirects
                .Select(x => new MugshotRedirectViewModel(x.ModName, x.Mugshots));
            MugshotRedirects = new(mugshotRedirects);
            WatchCollection(MugshotRedirects, SaveMugshotRedirects);
        }

        public void AckWelcome()
        {
            settings.Save();
            IsWelcomeScreen = false;
            WelcomeAcked?.Invoke(this, EventArgs.Empty);
        }

        public void AddBuildWarningSuppression()
        {
            BuildWarningWhitelist.Add(new(AvailablePlugins.FirstOrDefault()));
        }

        public void AddMugshotRedirect()
        {
            MugshotRedirects.Add(new("", ""));
        }

        public void RemoveBuildWarningSuppression(BuildWarningSuppressionViewModel suppressions)
        {
            BuildWarningWhitelist.Remove(suppressions);
        }

        public void RemoveMugshotRedirect(MugshotRedirectViewModel redirect)
        {
            MugshotRedirects.Remove(redirect);
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

        protected void OnMugshotsDirectoryChanged()
        {
            AvailableMugshotModNames = Directory.Exists(MugshotsDirectory) ?
                Directory.GetDirectories(MugshotsDirectory)
                    .Select(f => Path.GetRelativePath(MugshotsDirectory, f))
                    .ToArray() :
                Enumerable.Empty<string>();
        }

        private void SaveBuildWarningSuppressions()
        {
            settings.BuildWarningWhitelist = BuildWarningWhitelist
                .Select(x => new BuildWarningSuppression(x.PluginName, x.SelectedWarnings))
                .ToList();
            settings.Save();
            messageBus.Send(new SettingsChanged(SettingsChanged.SettingKind.BuildWarnings));
        }

        private void SaveMugshotRedirects()
        {
            settings.MugshotRedirects = MugshotRedirects
                .Select(x => new MugshotRedirect(x.ModName, x.Mugshots))
                .ToList();
            settings.Save();
            messageBus.Send(new SettingsChanged(SettingsChanged.SettingKind.MugshotSynonyms));
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

        private void WatchCollection<T>(ObservableCollection<T> collection, Action onChange)
            where T : INotifyPropertyChanged
        {
            PropertyChangedEventHandler propertyChangeHandler = (_, _) => onChange();
            collection.CollectionChanged += (_, e) =>
            {
                UpdateWatchedItems(e, propertyChangeHandler);
                onChange();
            };
            foreach (var entry in collection)
                entry.PropertyChanged += propertyChangeHandler;
        }

        private void UpdateWatchedItems(NotifyCollectionChangedEventArgs e, PropertyChangedEventHandler handler)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.Cast<INotifyPropertyChanged>())
                    item.PropertyChanged -= handler;
            if (e.NewItems != null)
                foreach (var item in e.NewItems.Cast<INotifyPropertyChanged>())
                    item.PropertyChanged += handler;
        }
    }

    public record BuildWarningSelection(BuildWarningId Id, bool IsSelected) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BuildWarningSuppressionViewModel : INotifyPropertyChanged
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

        public BuildWarningSuppressionViewModel(string pluginName, IEnumerable<BuildWarningId> warnings = null)
        {
            PluginName = pluginName;
            WarningSelections = Enum.GetValues<BuildWarningId>()
                .Where(x => x != BuildWarningId.Unknown)
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

    public class MugshotRedirectViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ModName { get; set; }
        public string Mugshots { get; set; }

        public MugshotRedirectViewModel(string modName, string mugshots)
        {
            ModName = modName;
            Mugshots = mugshots;
        }
    }
}

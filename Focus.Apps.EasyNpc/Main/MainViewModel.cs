using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.Maintenance;
using Focus.Apps.EasyNpc.Messages;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Main
{
    public class MainViewModel :
        IProfileContainer, IBuildContainer, IMaintenanceContainer, ISettingsContainer, ILogContainer,
        INotifyPropertyChanged
    {
        public class NavigationMenuItem
        {
            public string Name { get; private init; }
            // Stupid hack for https://github.com/Kinnara/ModernWpf/issues/389
            // Only workaround seems to be to use a two-way binding, but changing the type isn't actually supported.
            public Type PageType { get => pageType; set { } }
            public object ViewModel { get; private init; }

            private readonly Type pageType;

            public NavigationMenuItem(string name, object viewModel, Type pageType)
            {
                this.pageType = pageType;

                Name = name;
                ViewModel = viewModel;
            }
        }

        public delegate MainViewModel Factory(bool isFirstLaunch);

        public event PropertyChangedEventHandler? PropertyChanged;

        public BuildViewModel Build { get; private set; }
        public object Content { get; private set; }
        public LoaderViewModel Loader { get; private init; }
        public LogViewModel Log { get; private init; }
        public ILogger Logger { get; private init; }
        public MaintenanceViewModel Maintenance { get; private set; }
        public IReadOnlyList<NavigationMenuItem> NavigationMenuItems { get; private set; } =
            new List<NavigationMenuItem>().AsReadOnly();
        public ProfileViewModel Profile { get; private set; }
        public NavigationMenuItem SelectedNavigationMenuItem { get; set; }
        public SettingsViewModel Settings { get; private init; }
        public StartupReportViewModel StartupReport { get; private set; }

        public bool IsSettingsNavigationItemSelected
        {
            get { return SelectedNavigationMenuItem == settingsNavigationMenuItem; }
            set { SelectedNavigationMenuItem = settingsNavigationMenuItem; }
        }

        private readonly NavigationMenuItem settingsNavigationMenuItem;

        public MainViewModel(
            LoaderViewModel loader,
            LogViewModel log,
            SettingsViewModel settings,
            StartupReportViewModel.Factory startupReportFactory,
            ProfileViewModel.Factory profileFactory,
            BuildViewModel.Factory buildFactory,
            MaintenanceViewModel.Factory maintenanceFactory,
            IMessageBus messageBus,
            ILogger logger,
            bool isFirstLaunch)
        {
            Loader = loader;
            Log = log;
            Logger = logger;
            Settings = settings;

            Settings.IsWelcomeScreen = isFirstLaunch;
            Settings.WelcomeAcked += (sender, e) =>
            {
                Content = Loader;
            };

            settingsNavigationMenuItem = new("", Settings, typeof(SettingsPage));

            Content = isFirstLaunch ? Settings : Loader;

            Loader.Loaded += async () =>
            {
                var profileModel = await Loader.Tasks.Profile.ConfigureAwait(false);
                Profile = profileFactory(profileModel);
                StartupReport = startupReportFactory(profileModel);
                Build = buildFactory(profileModel);
                Maintenance = maintenanceFactory(profileModel);

                NavigationMenuItems = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem("Profile", Profile, typeof(ProfilePage)),
                    new NavigationMenuItem("Build", Build, typeof(BuildPage)),
                    new NavigationMenuItem("Maintenance", Maintenance, typeof(MaintenancePage)),
                    new NavigationMenuItem("Log", Log, typeof(LogPage)),
                }.AsReadOnly();
                SelectedNavigationMenuItem = NavigationMenuItems[0];

                Content = StartupReport.HasErrors ? StartupReport : this;
            };

            messageBus.Subscribe<JumpToNpc>(message =>
            {
                var found = Profile.SelectNpc(message.Key);
                if (found)
                    messageBus.Send(new NavigateToPage(MainPage.Profile));
            });
        }

        public void DismissStartupErrors()
        {
            Content = this;
        }
    }
}
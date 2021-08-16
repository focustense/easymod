using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.Maintenance;
using Focus.Apps.EasyNpc.Messages;
using Focus.Apps.EasyNpc.Profiles;
using PropertyChanged;
using Serilog;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Main
{
    public class MainViewModel :
        IProfileContainer, IBuildContainer, IMaintenanceContainer, ISettingsContainer, ILogContainer,
        INotifyPropertyChanged
    {
        public delegate MainViewModel Factory(bool isFirstLaunch);

        public event PropertyChangedEventHandler PropertyChanged;

        public BuildViewModel Build { get; private set; }
        public bool IsFirstLaunch { get; private set; }
        public bool IsLoaded { get; private set; }
        [DependsOn("IsFirstLaunch")]
        public bool IsNavigationVisible => !IsFirstLaunch;
        [DependsOn("IsFirstLaunch", "IsLoaded")]
        public bool IsReady => IsLoaded || IsFirstLaunch;
        public LoaderViewModel Loader { get; private init; }
        public LogViewModel Log { get; private init; }
        public ILogger Logger { get; private init; }
        public MaintenanceViewModel Maintenance { get; private set; }
        public ProfileViewModel Profile { get; private set; }
        public string PageTitle { get; set; }
        public SettingsViewModel Settings { get; private init; }

        public MainViewModel(
            LoaderViewModel loader,
            LogViewModel log,
            SettingsViewModel settings,
            ProfileViewModel.Factory profileFactory,
            BuildViewModel.Factory buildFactory,
            MaintenanceViewModel.Factory maintenanceFactory,
            IMessageBus messageBus,
            ILogger logger,
            bool isFirstLaunch)
        {
            IsFirstLaunch = isFirstLaunch;
            Loader = loader;
            Log = log;
            Logger = logger;
            Settings = settings;

            Settings.IsWelcomeScreen = isFirstLaunch;
            Settings.WelcomeAcked += (sender, e) =>
            {
                IsFirstLaunch = false;
            };

            Loader.Loaded += () =>
            {
                var profileModel = Loader.Tasks.Profile.Result;
                Profile = profileFactory(profileModel);
                Build = buildFactory(profileModel);
                Maintenance = maintenanceFactory(profileModel);

                IsLoaded = true;
            };

            messageBus.Subscribe<JumpToNpc>(message =>
            {
                var found = Profile.SelectNpc(message.Key);
                if (found)
                    messageBus.Send(new NavigateToPage(MainPage.Profile));
            });
        }
    }
}
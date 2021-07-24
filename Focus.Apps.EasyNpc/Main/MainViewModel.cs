using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.Maintenance;
using Focus.Apps.EasyNpc.Mutagen;
using Focus.Apps.EasyNpc.Nifly;
using Focus.Apps.EasyNpc.Profile;
using Focus.ModManagers;
using Mutagen.Bethesda.Plugins;
using PropertyChanged;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Main
{
    public abstract class MainViewModel<TKey> :
        INotifyPropertyChanged, IBuildContainer<TKey>, ILogContainer, IMaintenanceContainer<TKey>,
        IProfileContainer<TKey>, ISettingsContainer
        where TKey : struct
    {
        public event EventHandler<string> PageNavigationRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        public BuildViewModel<TKey> Build { get; private set; }
        public bool IsFirstLaunch { get; private set; }
        public bool IsLoaded { get; private set; }
        [DependsOn("IsFirstLaunch")]
        public bool IsNavigationVisible => !IsFirstLaunch;
        [DependsOn("IsFirstLaunch", "IsLoaded")]
        public bool IsReady => IsLoaded || IsFirstLaunch;
        public LoaderViewModel<TKey> Loader { get; private init; }
        public LogViewModel Log { get; private init; }
        public ILogger Logger { get; private init; }
        public MaintenanceViewModel<TKey> Maintenance { get; private set; }
        public ProfileViewModel<TKey> Profile { get; private set; }
        public string PageTitle { get; set; }
        public SettingsViewModel Settings { get; private init; }

        protected IModResolver ModResolver { get; private init; }

        private readonly IGameDataEditor<TKey> gameDataEditor;

        public MainViewModel(IModResolver modResolver, bool isFirstLaunch, bool debugMode)
        {
            ModResolver = modResolver;

            IsFirstLaunch = isFirstLaunch;

            var loggingLevelSwitch =
                new LoggingLevelSwitch(debugMode ? LogEventLevel.Debug : LogEventLevel.Information);
            var logViewModelSink = new LogViewModelSink();
            Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.File(ProgramData.LogFileName,
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromMilliseconds(500))
                .WriteTo.Sink(logViewModelSink)
                .CreateLogger();

            if (debugMode)
                Logger.Debug("Debug mode enabled");

            gameDataEditor = CreateEditor();

            Log = new LogViewModel(gameDataEditor.Log);
            logViewModelSink.ViewModel = Log;
            Logger.Information(
                "Initialized: {appName:l} version {version:l}, built on {buildDate}",
                AssemblyProperties.Name, AssemblyProperties.Version, AssemblyProperties.BuildTimestampUtc);
            Log.ResumeExternalMonitoring();

            Settings = new SettingsViewModel(modResolver) { IsWelcomeScreen = isFirstLaunch };
            Settings.WelcomeAcked += (sender, e) =>
            {
                IsFirstLaunch = false;
            };
            Loader = new LoaderViewModel<TKey>(gameDataEditor, Log, Logger);
            Loader.Loaded += () => {
                Log.PauseExternalMonitoring();
                Settings.AvailablePlugins = Loader.LoadedPluginNames;
                var profileEventLog = new ProfileEventLog(ProgramData.ProfileLogFileName);
                Profile = new ProfileViewModel<TKey>(
                    Loader.Npcs, Loader.ModPluginMapFactory, Loader.LoadedPluginNames, Loader.LoadedMasterNames,
                    profileEventLog);
                var npcConfigs = Profile.GetAllNpcConfigurations();
                Maintenance = new MaintenanceViewModel<TKey>(npcConfigs, profileEventLog, Loader.LoadedPluginNames);
                var wigResolver = new SimpleWigResolver<TKey>(Loader.Hairs);
                var faceGenEditor = new NiflyFaceGenEditor(Logger);
                var buildChecker = new BuildChecker<TKey>(
                    Loader.LoadedPluginNames, Loader.Graph, npcConfigs, Profile.RuleSet, modResolver,
                    Loader.ModPluginMapFactory, gameDataEditor.ArchiveProvider, profileEventLog, Logger);
                Build = new BuildViewModel<TKey>(
                    gameDataEditor.ArchiveProvider, buildChecker, gameDataEditor.MergedPluginBuilder,
                    Loader.ModPluginMapFactory, modResolver, Profile.GetAllNpcConfigurations(), wigResolver,
                    faceGenEditor, Logger);
                Build.WarningExpanded += BuildViewModel_WarningExpanded;
                IsLoaded = true;
            };
        }

        protected abstract IGameDataEditor<TKey> CreateEditor();

        private void BuildViewModel_WarningExpanded(object sender, BuildWarning e)
        {
            var found = Profile.SelectNpc(e.RecordKey);
            if (found)
                PageNavigationRequested?.Invoke(this, "profile");
        }
    }

    public class MainViewModel : MainViewModel<FormKey>
    {
        public MainViewModel(IModResolver modResolver, bool isFirstLaunch = false, bool debugMode = false)
            : base(modResolver, isFirstLaunch, debugMode) { }

        protected override IGameDataEditor<FormKey> CreateEditor()
        {
            return new MutagenAdapter(ModResolver, Logger);
        }
    }
}
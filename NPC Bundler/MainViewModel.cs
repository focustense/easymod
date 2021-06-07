using Mutagen.Bethesda;
using Serilog;
using System;
using System.ComponentModel;

namespace NPC_Bundler
{
    public abstract class MainViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BuildViewModel<TKey> Build { get; private set; }
        public bool IsReady { get; private set; }
        public LoaderViewModel<TKey> Loader { get; private init; }
        public LogViewModel Log { get; private init; }
        public ILogger Logger { get; private init; }
        public MaintenanceViewModel<TKey> Maintenance { get; private set; }
        public ProfileViewModel<TKey> Profile { get; private set; }
        public string PageTitle { get; set; }
        public SettingsViewModel Settings { get; private init; }

        private readonly IGameDataEditor<TKey> gameDataEditor;

        public MainViewModel()
        {
            var logViewModelSink = new LogViewModelSink();
            Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(ProgramData.LogFileName,
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromMilliseconds(500))
                .WriteTo.Sink(logViewModelSink)
                .CreateLogger();

            gameDataEditor = CreateEditor();

            Log = new LogViewModel(gameDataEditor.Log);
            logViewModelSink.ViewModel = Log;
            Logger.Information("Initialized");
            Log.ResumeExternalMonitoring();

            Settings = new SettingsViewModel();
            Loader = new LoaderViewModel<TKey>(gameDataEditor, Log, Logger);
            Loader.Loaded += () => {
                Log.PauseExternalMonitoring();
                Settings.AvailablePlugins = Loader.LoadedPluginNames;
                var profileEventLog = new ProfileEventLog(ProgramData.GetProfileLogFileName());
                Profile = new ProfileViewModel<TKey>(
                    Loader.Npcs, Loader.ModPluginMapFactory, Loader.LoadedMasterNames, profileEventLog);
                Maintenance = new MaintenanceViewModel<TKey>(Profile.GetAllNpcConfigurations(), profileEventLog);
                var archiveFileMap = new ArchiveFileMap(gameDataEditor.ArchiveProvider);
                var wigResolver = new SimpleWigResolver<TKey>(Loader.Hairs);
                var faceGenEditor = new NiflyFaceGenEditor(Logger);
                Build = new BuildViewModel<TKey>(
                    gameDataEditor.ArchiveProvider, gameDataEditor.MergedPluginBuilder, Loader.ModPluginMapFactory,
                    Profile.GetAllNpcConfigurations(), wigResolver, faceGenEditor, archiveFileMap, Logger);
                IsReady = true;
            };
        }

        protected abstract IGameDataEditor<TKey> CreateEditor();
    }

    public class MainViewModel : MainViewModel<FormKey>
    {
        protected override IGameDataEditor<FormKey> CreateEditor()
        {
            return new MutagenAdapter(Logger);
        }
    }
}
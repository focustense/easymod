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
        public LoaderViewModel<TKey> Loader { get; init; }
        public LogViewModel Log { get; init; }
        public ProfileViewModel<TKey> Profile { get; private set; }
        public string PageTitle { get; set; }
        public SettingsViewModel Settings { get; init; }

        private readonly IGameDataEditor<TKey> gameDataEditor;

        public MainViewModel()
        {
            var logViewModelSink = new LogViewModelSink();
            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(ProgramData.GetLogFileName())
                .WriteTo.Sink(logViewModelSink)
                .CreateLogger();

            gameDataEditor = CreateEditor();

            Log = new LogViewModel(gameDataEditor.Log);
            logViewModelSink.ViewModel = Log;
            logger.Information("Initialized");
            Log.ResumeExternalMonitoring();

            Settings = new SettingsViewModel();
            Loader = new LoaderViewModel<TKey>(gameDataEditor, Log, logger);
            Loader.Loaded += () => {
                Log.PauseExternalMonitoring();
                Settings.AvailablePlugins = Loader.LoadedPluginNames;
                Profile = new ProfileViewModel<TKey>(Loader.Npcs, Loader.ModPluginMapFactory, Loader.LoadedMasterNames);
                var archiveFileMap = new ArchiveFileMap(gameDataEditor.ArchiveProvider);
                Build = new BuildViewModel<TKey>(
                    gameDataEditor.ArchiveProvider, gameDataEditor.MergedPluginBuilder, Loader.ModPluginMapFactory,
                    Profile.GetAllNpcConfigurations(), archiveFileMap);
                IsReady = true;
            };
        }

        protected abstract IGameDataEditor<TKey> CreateEditor();
    }

#if MUTAGEN
    public class MainViewModel : MainViewModel<FormKey>
    {
        protected override IGameDataEditor<FormKey> CreateEditor()
        {
            return new MutagenAdapter();
        }
    }
#else
    public class MainViewModel : MainViewModel<uint>
    {
        protected override IGameDataEditor<uint> CreateEditor()
        {
            return new XEditGameDataEditor();
        }
    }
#endif
}

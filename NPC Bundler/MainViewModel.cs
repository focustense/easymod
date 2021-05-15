using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BuildViewModel Build { get; private set; }
        public bool IsReady { get; private set; }
        public LoaderViewModel Loader { get; init; }
        public LogViewModel Log { get; init; }
        public ProfileViewModel Profile { get; private set; }
        public string PageTitle { get; set; }
        public SettingsViewModel Settings { get; init; }

        public MainViewModel()
        {
            Log = new LogViewModel();
            Settings = new SettingsViewModel();
            Loader = new LoaderViewModel(Log);
            Loader.Loaded += () => {
                Profile = new ProfileViewModel(Loader.Npcs);
                Build = new BuildViewModel(Profile.GetAllNpcConfigurations(), Loader.LoadedPluginNames);
                IsReady = true;
            };
        }
    }
}

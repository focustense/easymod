using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Dictionary<string, NavLink> NavLinks = new()
        {
            { "profile", new NavLink("Profile", typeof(ProfilePage), x => x.Profile) },
            { "build", new NavLink("Build", typeof(BuildPage), x => x.Build) },
            { "log", new NavLink("Log", typeof(LogPage), x => x.Log) },
            { "settings", new NavLink("Settings", typeof(SettingsPage), x => x.Settings) },
        };

        private readonly MainViewModel model;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = model = new MainViewModel();
        }

        private void Navigate(string pageName)
        {
            if (NavLinks.TryGetValue(pageName, out NavLink navLink))
            {
                model.PageTitle = navLink.Title;
                PageFrame.Navigate(navLink.PageType);
            }
        }

        private void NavigationView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                Navigate("settings");
            else
                Navigate((string)args.SelectedItemContainer.Tag);
        }
    }

    record NavLink(string Title, Type PageType, Func<MainViewModel, object> ViewModelSelector);
}

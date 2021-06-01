using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

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
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    model.Logger.Error(e.ExceptionObject as Exception, "Exception was not handled");
                    var crashViewModel = new CrashViewModel(
                        ProgramData.DirectoryPath, Path.GetFileName(ProgramData.GetLogFileName()));
                    var errorWindow = new ErrorWindow { DataContext = crashViewModel, Owner = this };
                    errorWindow.ShowDialog();
                }
                catch (Exception)
                {
                    // The ship is going down and we're out of lifeboats.
                }
                Application.Current.Shutdown();
            };
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

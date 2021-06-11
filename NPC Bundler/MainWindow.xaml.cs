using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Focus.Apps.EasyNpc
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
            { "maintenance", new NavLink("Maintenance", typeof(MaintenancePage), x => x.Maintenance) },
            { "log", new NavLink("Log", typeof(LogPage), x => x.Log) },
            { "settings", new NavLink("Settings", typeof(SettingsPage), x => x.Settings) },
        };

        private readonly MainViewModel model;

        public MainWindow(MainViewModel model)
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    model.Logger.Error(e.ExceptionObject as Exception, "Exception was not handled");
                    var crashViewModel = new CrashViewModel(
                        ProgramData.DirectoryPath, Path.GetFileName(ProgramData.LogFileName));
                    var errorWindow = new ErrorWindow { DataContext = crashViewModel, Owner = this };
                    errorWindow.ShowDialog();
                }
                catch (Exception)
                {
                    // The ship is going down and we're out of lifeboats.
                }
                Application.Current.Shutdown();
            };
            DataContext = this.model = model;
            if (model.IsFirstLaunch)
            {
                foreach (NavigationViewItem navItem in MainNavigationView.MenuItems)
                    navItem.IsSelected = false;
                Navigate("settings");
                model.Settings.WelcomeAcked += (sender, e) =>
                    (MainNavigationView.MenuItems[0] as NavigationViewItem).IsSelected = true;
            }
        }

        private void Navigate(string pageName)
        {
            if (NavLinks.TryGetValue(pageName, out NavLink navLink))
            {
                model.PageTitle = navLink.Title;
                PageFrame.Navigate(navLink.PageType);
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                Navigate("settings");
            else
                Navigate((string)args.SelectedItemContainer.Tag);
        }
    }

    record NavLink(string Title, Type PageType, Func<MainViewModel, object> ViewModelSelector);
}

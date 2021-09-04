using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using ModernWpf.Controls;
using System;
using System.IO;
using System.Windows;

namespace Focus.Apps.EasyNpc.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        }

        // Ugly code-behind hack for ModernWpf not allowing us to supply our own Settings item.
        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                model.IsSettingsNavigationItemSelected = true;
            else if (args.SelectedItem is MainViewModel.NavigationMenuItem item)
                model.SelectedNavigationMenuItem = item;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    record NavLink(string Title, Type PageType, Func<MainViewModel, object> ViewModelSelector);
}

using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Focus.Apps.EasyNpc
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var isFirstLaunch =
                e.Args.Contains("/forceintro", StringComparer.OrdinalIgnoreCase) ||
                (string.IsNullOrEmpty(BundlerSettings.Default.ModRootDirectory) &&
                    !File.Exists(ProgramData.ProfileLogFileName));
            var mainViewModel = new MainViewModel(isFirstLaunch);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}

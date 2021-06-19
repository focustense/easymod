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
            if (BundlerSettings.Default.UpgradeRequired)
            {
                BundlerSettings.Default.Upgrade();
                BundlerSettings.Default.UpgradeRequired = false;
                BundlerSettings.Default.Save();
            }

            var isFirstLaunch =
                e.Args.Contains("/forceintro", StringComparer.OrdinalIgnoreCase) ||
                (string.IsNullOrEmpty(BundlerSettings.Default.ModRootDirectory) &&
                    !File.Exists(ProgramData.ProfileLogFileName));
            var debugMode = e.Args.Contains("/debug", StringComparer.OrdinalIgnoreCase);
            var mainViewModel = new MainViewModel(isFirstLaunch, debugMode);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}

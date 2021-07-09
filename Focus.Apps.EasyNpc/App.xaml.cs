using CommandLine;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
using Focus.ModManagers;
using Focus.ModManagers.Vortex;
using System;
using System.Diagnostics;
using System.IO;
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
            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(Start);
        }

        private void Start(CommandLineOptions options)
        {
            var isFirstLaunch =
                options.ForceIntro ||
                (string.IsNullOrEmpty(Settings.Default.ModRootDirectory) &&
                    !File.Exists(ProgramData.ProfileLogFileName));
            var isVortexProcess = string.Equals(
                Process.GetCurrentProcess().Parent()?.ProcessName, "vortex", StringComparison.OrdinalIgnoreCase);
            var isVortexDirectory =
                !string.IsNullOrEmpty(Settings.Default.ModRootDirectory) &&
                File.Exists(Path.Combine(Settings.Default.ModRootDirectory, "__vortex_staging_folder"));
            var isVortex = isVortexProcess || isVortexDirectory;
            if (isVortex && string.IsNullOrEmpty(options.VortexManifest) &&
                !Warn(StartupWarnings.MissingVortexManifest))
            {
                Current.Shutdown();
                return;
            }
            var defaultModResolver = new PassthroughModResolver();
            IModResolver modResolver = !string.IsNullOrEmpty(options.VortexManifest) ?
                new VortexModResolver(defaultModResolver, options.VortexManifest) : defaultModResolver;
            if (isFirstLaunch && string.IsNullOrEmpty(Settings.Default.ModRootDirectory))
            {
                Settings.Default.ModRootDirectory = modResolver.GetDefaultModRootDirectory();
                if (!string.IsNullOrEmpty(Settings.Default.ModRootDirectory))
                    Settings.Default.Save();
            }
            var mainViewModel = new MainViewModel(modResolver, isFirstLaunch, options.DebugMode);
            var mainWindow = MainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }

        private static bool Warn(StartupWarning warning)
        {
            var model = new StartupWarningViewModel
            {
                Title = warning.Title,
                Content = warning.Description,
            };
            var warningWindow = new StartupWarningWindow
            {
                DataContext = model
            };
            return warningWindow.ShowDialog() == true;
        }
    }
}

using CommandLine;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
using Focus.ModManagers;
using Focus.ModManagers.ModOrganizer;
using Focus.ModManagers.Vortex;
using System;
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
            Console.WriteLine("Application started");
            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(Start);
        }

        private void Start(CommandLineOptions options)
        {
            Settings.Default.BuildReportPath = options.ReportPath;
            var startupInfo = StartupInfo.Detect();
            var isFirstLaunch =
                options.ForceIntro ||
                (startupInfo.ModDirectoryOwner == ModManager.None && !File.Exists(ProgramData.ProfileLogFileName));
            var isVortex =
                startupInfo.Launcher == ModManager.Vortex || startupInfo.ModDirectoryOwner == ModManager.Vortex;
            if (isVortex && string.IsNullOrEmpty(options.VortexManifest) &&
                !Warn(StartupWarnings.MissingVortexManifest))
            {
                Current.Shutdown();
                return;
            }
            var modResolver = CreateModResolver(startupInfo, options);
            if (isFirstLaunch && string.IsNullOrEmpty(Settings.Default.ModRootDirectory))
                Settings.Default.ModRootDirectory = modResolver.GetDefaultModRootDirectory();
            var mainViewModel = new MainViewModel(modResolver, isFirstLaunch, options.DebugMode);
            var mainWindow = MainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }

        private static IModResolver CreateModResolver(StartupInfo startupInfo, CommandLineOptions options)
        {
            var defaultModResolver = new PassthroughModResolver();
            // Vortex manifest is a command-line option, so it automatically overrides detection-based mechanisms.
            if (!string.IsNullOrEmpty(options.VortexManifest))
                return new VortexModResolver(defaultModResolver, options.VortexManifest);
            switch (startupInfo.Launcher)
            {
                case ModManager.ModOrganizer:
                    return new ModOrganizerModResolver(startupInfo.ParentProcessPath);
                default:
                    return defaultModResolver;
            }
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

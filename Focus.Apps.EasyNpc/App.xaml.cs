﻿using CommandLine;
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
            if (!string.IsNullOrEmpty(options.ReportPath))
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
            try
            {
                var mainViewModel =
                    new MainViewModel(options.GameName, options.GamePath, modResolver, isFirstLaunch, options.DebugMode);
                var mainWindow = MainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (MissingGameDataException ex)
            {
                Warn(StartupWarnings.MissingGameData(ex.GameId, ex.GameName), true);
                Current.Shutdown();
            }
            catch (UnsupportedGameException ex)
            {
                Warn(StartupWarnings.UnsupportedGame(ex.GameId, ex.GameName), true);
                Current.Shutdown();
            }
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
                    var config = IniConfiguration.AutoDetect(startupInfo.ParentProcessPath);
                    return new ModOrganizerModResolver(config);
                default:
                    return defaultModResolver;
            }
        }

        private static bool Warn(StartupWarning warning, bool isFatal = false)
        {
            var model = new StartupWarningViewModel
            {
                Title = warning.Title,
                Content = warning.Description,
                IsFatal = isFatal,
            };
            var warningWindow = new StartupWarningWindow
            {
                DataContext = model
            };
            return warningWindow.ShowDialog() == true;
        }
    }
}

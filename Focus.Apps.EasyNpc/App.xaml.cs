using Autofac;
using Autofac.Core;
using CommandLine;
using CommandLine.Text;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
using Focus.Apps.EasyNpc.Reports;
using Focus.ModManagers;
using Serilog;
using System;
using System.Collections.Generic;
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
        private IDisposable? container;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Application started");
            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(Start)
                .WithNotParsed(DisplayErrors);
        }

        private void DisplayErrors(IEnumerable<Error> errors)
        {
            var sentenceBuilder = SentenceBuilder.Create();
            var errorMessages = errors.Select(e => sentenceBuilder.FormatError(e));
            Warn(StartupWarnings.InvalidCommandLine(errorMessages), true);
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
                return;
            }

            var container = AppContainer.Build(options, startupInfo);
            this.container = container;
            var logger = container.Resolve<ILogger>();
            var mainWindow = MainWindow = new MainWindow(logger, container);
            if (isFirstLaunch && string.IsNullOrEmpty(Settings.Default.DefaultModRootDirectory))
                Settings.Default.DefaultModRootDirectory = container.Resolve<IModManagerConfiguration>().ModsDirectory;
            try
            {
                try
                {
                    if (options.PostBuild)
                    {
                        var postBuildReportViewModel = container.Resolve<PostBuildReportViewModel>();
                        mainWindow.DataContext = postBuildReportViewModel;
                        _ = postBuildReportViewModel.UpdateReport();
                    }
                    else
                    {
                        var mainViewModelFactory = container.Resolve<MainViewModel.Factory>();
                        var mainViewModel = mainViewModelFactory(isFirstLaunch);
                        mainWindow.DataContext = mainViewModel;
                    }
                }
                catch (DependencyResolutionException ex)
                {
                    if (ex.InnerException is not null)
                        throw ex.InnerException;
                    throw;
                }
                mainWindow.Show();
            }
            catch (MissingGameDataException ex)
            {
                Warn(StartupWarnings.MissingGameData(ex.GameId, ex.GameName), true);
            }
            catch (UnsupportedGameException ex)
            {
                Warn(StartupWarnings.UnsupportedGame(ex.GameId, ex.GameName), true);
            }
        }

        private bool Warn(StartupWarning warning, bool isFatal = false)
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
            var ignored = warningWindow.ShowDialog() == true;
            if (!ignored)
            {
                container?.Dispose();
                Current.Shutdown();
            }
            return ignored;
        }
    }
}

using CommandLine;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
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
            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(Start);
        }

        private void Start(CommandLineOptions options)
        {
            var isFirstLaunch =
                options.ForceIntro ||
                (string.IsNullOrEmpty(Settings.Default.ModRootDirectory) &&
                    !File.Exists(ProgramData.ProfileLogFileName));
            var mainViewModel = new MainViewModel(isFirstLaunch, options.DebugMode);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}

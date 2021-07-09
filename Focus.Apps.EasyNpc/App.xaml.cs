using CommandLine;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Main;
using Focus.ModManagers;
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
            Parser.Default.ParseArguments<CommandLineOptions>(e.Args)
                .WithParsed(Start);
        }

        private void Start(CommandLineOptions options)
        {
            var isFirstLaunch =
                options.ForceIntro ||
                (string.IsNullOrEmpty(Settings.Default.ModRootDirectory) &&
                    !File.Exists(ProgramData.ProfileLogFileName));
            var defaultModResolver = new PassthroughModResolver();
            IModResolver modResolver = !string.IsNullOrEmpty(options.VortexManifest) ?
                new VortexModResolver(defaultModResolver, options.VortexManifest) : defaultModResolver;
            var mainViewModel = new MainViewModel(modResolver, isFirstLaunch, options.DebugMode);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}

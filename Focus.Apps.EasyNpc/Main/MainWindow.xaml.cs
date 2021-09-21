using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using Serilog;
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
        public MainWindow(ILogger logger)
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    logger.Error(e.ExceptionObject as Exception, "Exception was not handled");
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
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

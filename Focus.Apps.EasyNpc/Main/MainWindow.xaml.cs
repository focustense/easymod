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
        private readonly IDisposable container;

        public MainWindow(ILogger logger, IDisposable container)
        {
            this.container = container;
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
                container.Dispose();
                Application.Current.Shutdown();
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowChromeFix.Install(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            container.Dispose();
            Application.Current.Shutdown();
        }
    }
}

using ModernWpf.Controls;
using System.Windows;

namespace Focus.Apps.EasyNpc.Debug
{
    /// <summary>
    /// Interaction logic for LogPage.xaml
    /// </summary>
    public partial class LogPage : Page
    {
        protected LogViewModel Model => ((ILogContainer)DataContext)?.Log;

        public LogPage()
        {
            InitializeComponent();
        }

        private void Page_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null && e.OldValue is ILogContainer container)
                container.Log.PauseExternalMonitoring();
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Model.ResumeExternalMonitoring();
        }
    }
}

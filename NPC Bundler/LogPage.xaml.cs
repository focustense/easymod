using ModernWpf.Controls;
using System.Windows;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for LogPage.xaml
    /// </summary>
    public partial class LogPage : Page
    {
        protected LogViewModel Model => ((MainViewModel)DataContext)?.Log;

        public LogPage()
        {
            InitializeComponent();
        }

        private void Page_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null && e.OldValue is MainViewModel model)
                model.Log.Pause();
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Model.Resume();
        }
    }
}

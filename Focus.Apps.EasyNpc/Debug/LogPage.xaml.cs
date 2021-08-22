using ModernWpf.Controls;

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
    }
}

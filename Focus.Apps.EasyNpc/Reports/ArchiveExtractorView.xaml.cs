using System.Threading.Tasks;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Reports
{
    /// <summary>
    /// Interaction logic for ArchiveExtractorView.xaml
    /// </summary>
    public partial class ArchiveExtractorView : UserControl
    {
        protected ArchiveExtractorViewModel Model => (ArchiveExtractorViewModel)DataContext;

        public ArchiveExtractorView()
        {
            InitializeComponent();
        }

        private void ExtractButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Model.ExtractAll();
        }
    }
}

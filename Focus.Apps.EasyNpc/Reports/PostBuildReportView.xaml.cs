using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Reports
{
    /// <summary>
    /// Interaction logic for PostBuildReportView.xaml
    /// </summary>
    public partial class PostBuildReportView : UserControl
    {
        protected PostBuildReportViewModel Model => (PostBuildReportViewModel)DataContext;

        public PostBuildReportView()
        {
            InitializeComponent();
        }

        private void ExtractArchivesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Model.ExtractConflictingFiles();
        }
    }
}

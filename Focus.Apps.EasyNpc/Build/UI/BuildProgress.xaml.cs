using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Focus.Apps.EasyNpc.Build.UI
{
    /// <summary>
    /// Interaction logic for BuildProgress.xaml
    /// </summary>
    public partial class BuildProgress : UserControl
    {
        public BuildProgress()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var headerSeparator = TaskGrid
                .FindVisualChildrenByName<Rectangle>("ColumnHeadersAndRowsSeparator")
                .FirstOrDefault();
            if (headerSeparator == null)
                return;
            headerSeparator.Margin = new Thickness(0, 0, 16, 0);
        }
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private void TaskGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (sender is not Control ctrl || ctrl.Parent is not UIElement parentElement)
                return;
            var bubbleArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };
            parentElement.RaiseEvent(bubbleArgs);
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

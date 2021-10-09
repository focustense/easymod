using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Focus.Apps.EasyNpc
{
    public class WheelBubblingBehavior : Behavior<FrameworkElement>
    {
        private ScrollViewer? scrollViewer;

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = GetScrollViewer();
            if (scrollViewer is null)
                return;
            var currentOffset = scrollViewer!.VerticalOffset;
            if ((e.Delta > 0 && currentOffset <= 0) || (e.Delta < 0 && currentOffset >= scrollViewer.ScrollableHeight))
                ForwardEvent(e);
        }

        private void ForwardEvent(MouseWheelEventArgs originalEvent)
        {
            var e = new MouseWheelEventArgs(originalEvent.MouseDevice, originalEvent.Timestamp, originalEvent.Delta);
            e.RoutedEvent = UIElement.MouseWheelEvent;
            AssociatedObject.RaiseEvent(e);
        }

        private ScrollViewer? GetScrollViewer()
        {
            if (scrollViewer is null)
                scrollViewer = AssociatedObject.FindVisualChildrenByType<ScrollViewer>().FirstOrDefault();
            return scrollViewer;
        }
    }
}

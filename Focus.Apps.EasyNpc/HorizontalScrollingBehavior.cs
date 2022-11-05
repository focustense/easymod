using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Focus.Apps.EasyNpc
{
    public class HorizontalScrollingBehavior : Behavior<FrameworkElement>
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
            if (scrollViewer is null || !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;
            if (e.Delta < 0)
                scrollViewer.LineRight();
            else
                scrollViewer.LineLeft();
            e.Handled = true;
        }

        private ScrollViewer? GetScrollViewer()
        {
            if (scrollViewer is null)
                scrollViewer = AssociatedObject.FindVisualChildrenByType<ScrollViewer>().FirstOrDefault();
            return scrollViewer;
        }
    }
}

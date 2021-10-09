using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc
{
    public class ScrollRestoreBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(string), typeof(ScrollRestoreBehavior), new PropertyMetadata(""));

        private static readonly Dictionary<string, Point> savedPositions = new();

        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        private ScrollViewer? scrollViewer;

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            RestoreScrollPosition();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveScrollPosition();
        }

        private ScrollViewer? GetScrollViewer()
        {
            if (scrollViewer is null)
                scrollViewer = AssociatedObject.FindVisualChildrenByType<ScrollViewer>().FirstOrDefault();
            return scrollViewer;
        }

        private void RestoreScrollPosition()
        {
            if (!savedPositions.TryGetValue(Key, out var position))
                return;
            var scrollViewer = GetScrollViewer();
            if (scrollViewer is not null)
            {
                scrollViewer.ScrollToHorizontalOffset(position.X);
                scrollViewer.ScrollToVerticalOffset(position.Y);
            }
        }

        private void SaveScrollPosition()
        {
            var scrollViewer = GetScrollViewer();
            if (scrollViewer is not null)
                savedPositions[Key] = new(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
        }
    }
}

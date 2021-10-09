using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Focus.Apps.EasyNpc
{
    public static class ScrollAnimation
    {
        public static readonly DependencyProperty TimeDurationProperty = DependencyProperty.RegisterAttached(
            "TimeDuration", typeof(TimeSpan), typeof(ScrollAnimation), new(TimeSpan.FromMilliseconds(200)));

        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached(
            "VerticalOffset", typeof(double), typeof(ScrollAnimation),
            new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static Task AnimateVertical(
            ScrollViewer scrollViewer, double offset, CancellationToken? cancellationToken = null)
        {
            var cts = new TaskCompletionSource();
            var animation = new DoubleAnimation
            {
                From = scrollViewer.VerticalOffset,
                To = Math.Min(offset, scrollViewer.ScrollableHeight),
                Duration = GetTimeDuration(scrollViewer),
                EasingFunction = new QuarticEase(),
            };
            var storyboard = new Storyboard { Children = { animation } };
            Storyboard.SetTarget(animation, scrollViewer);
            Storyboard.SetTargetProperty(animation, new(VerticalOffsetProperty));
            storyboard.Completed += (_, _) => cts.TrySetResult();
            if (cancellationToken.HasValue)
                cancellationToken.Value.Register(() =>
                {
                    storyboard.Stop();
                    cts.TrySetCanceled();
                });
            storyboard.Begin(scrollViewer, true);
            return cts.Task;
        }

        public static TimeSpan GetTimeDuration(FrameworkElement target)
        {
            return (TimeSpan)target.GetValue(TimeDurationProperty);
        }

        public static double GetVerticalOffset(FrameworkElement target)
        {
            return (double)target.GetValue(VerticalOffsetProperty);
        }

        public static void SetTimeDuration(FrameworkElement target, TimeSpan value)
        {
            target.SetValue(TimeDurationProperty, value);
        }

        public static void SetVerticalOffset(FrameworkElement target, double value)
        {
            target.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not ScrollViewer scrollViewer)
                return;
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}

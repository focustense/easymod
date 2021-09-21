using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Focus.Apps.EasyNpc
{
    public static class ForceTextTrimming
    {
        public static TextTrimming GetForceTextTrimming(DependencyObject obj)
        {
            return (TextTrimming)obj.GetValue(ForceTextTrimmingProperty);
        }

        public static void SetForceTextTrimming(DependencyObject obj, TextTrimming value)
        {
            obj.SetValue(ForceTextTrimmingProperty, value);
        }

        public static readonly DependencyProperty ForceTextTrimmingProperty =
            DependencyProperty.RegisterAttached(
                "ForceTextTrimming", typeof(TextTrimming), typeof(ForceTextTrimming),
                new PropertyMetadata(ForceTextTrimmingChanged));

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                var fe = (FrameworkElement)sender;
                var textTrimming = GetForceTextTrimming(fe);
                UpdateAll(fe, textTrimming);
            }));
        }

        private static void ForceTextTrimmingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is FrameworkElement fe)
                WatchDataContext(fe);
            UpdateAll(obj, (TextTrimming)e.NewValue);
        }

        private static void UpdateAll(DependencyObject obj, TextTrimming value)
        {
            foreach (var textBlock in obj.FindVisualChildrenByType<TextBlock>())
                textBlock.TextTrimming = value;
        }

        private static void WatchDataContext(FrameworkElement fe)
        {
            fe.Loaded -= Element_Loaded;
            fe.Loaded += Element_Loaded;
        }
    }
}

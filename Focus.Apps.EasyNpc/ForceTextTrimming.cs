using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Focus.Apps.EasyNpc
{
    public static class ForceTextTrimming
    {
        private static readonly ConditionalWeakTable<FrameworkElement, object> pendingUpdates = new();

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

        private static void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            ScheduleUpdate(e.Row, GetForceTextTrimming((DependencyObject)sender!));
        }

        private static void Element_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScheduleUpdate((FrameworkElement)sender);
        }

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            ScheduleUpdate((FrameworkElement)sender);
        }

        private static void ForceTextTrimmingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is FrameworkElement fe)
                WatchDataContext(fe);
            UpdateAll(obj, (TextTrimming)e.NewValue);
        }

        private static void ScheduleUpdate(FrameworkElement fe, TextTrimming? textTrimming = null)
        {
            if (pendingUpdates.TryGetValue(fe, out _))
                return;
            pendingUpdates.Add(fe, new object());
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                if (textTrimming == null)
                    textTrimming = GetForceTextTrimming(fe);
                UpdateAll(fe, textTrimming.Value);
                pendingUpdates.Remove(fe);
            }));
        }

        private static void UpdateAll(DependencyObject obj, TextTrimming value)
        {
            foreach (var textBlock in obj.FindVisualChildrenByType<TextBlock>())
                textBlock.TextTrimming = value;
        }

        private static void WatchDataContext(FrameworkElement fe)
        {
            fe.DataContextChanged -= Element_DataContextChanged;
            fe.DataContextChanged += Element_DataContextChanged;
            fe.Loaded -= Element_Loaded;
            fe.Loaded += Element_Loaded;
            if (fe is DataGrid dg)
            {
                dg.LoadingRow -= DataGrid_LoadingRow;
                dg.LoadingRow += DataGrid_LoadingRow;
            }
        }
    }
}

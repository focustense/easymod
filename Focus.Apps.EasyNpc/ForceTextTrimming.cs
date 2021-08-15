using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc
{
    public static class ForceTextTrimming
    {
        public static TextTrimming GetCanForceTextTrimming(DependencyObject obj)
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

        private static void ForceTextTrimmingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            foreach (var textBlock in obj.FindVisualChildrenByType<TextBlock>())
                textBlock.TextTrimming = (TextTrimming)e.NewValue;
        }
    }
}

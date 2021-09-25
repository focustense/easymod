using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc
{
    public class BorderProperties
    {
        public static CornerRadius GetCornerRadius(DependencyObject obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                nameof(BorderProperties), typeof(CornerRadius), typeof(BorderProperties),
                new UIPropertyMetadata(new CornerRadius(-1), CornerRadiusChanged));

        public static void CornerRadiusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement fe)
                return;
            fe.Loaded -= Element_Loaded;
            fe.Loaded += Element_Loaded;
            UpdateCornerRadius(fe);
        }

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe)
                return;
            UpdateCornerRadius(fe);
        }

        private static void UpdateCornerRadius(FrameworkElement fe)
        {
            foreach (var border in fe.FindVisualChildrenByType<Border>())
                border.CornerRadius = GetCornerRadius(fe);
        }
    }
}

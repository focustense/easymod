using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace Focus.Apps.EasyNpc
{
    public static class HyperlinkExtensions
    {
        public static readonly DependencyProperty DefaultNavigationProperty = DependencyProperty.RegisterAttached(
            "DefaultNavigation",
            typeof(bool),
            typeof(HyperlinkExtensions),
            new UIPropertyMetadata(false, OnDefaultNavigationChanged));

        public static bool GetDefaultNavigation(DependencyObject obj)
        {
            return (bool)obj.GetValue(DefaultNavigationProperty);
        }

        public static void SetDefaultNavigation(DependencyObject obj, bool value)
        {
            obj.SetValue(DefaultNavigationProperty, value);
        }

        private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private static void OnDefaultNavigationChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is not Hyperlink hyperlink)
                return;
            if ((bool)args.NewValue)
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            else
                hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
        }
    }
}

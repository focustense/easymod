using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Main
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        protected MainViewModel Model => (MainViewModel)DataContext;

        public MainView()
        {
            InitializeComponent();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Model.DismissStartupErrors();
        }

        // Ugly code-behind hack for ModernWpf not allowing us to supply our own Settings item.
        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                Model.IsSettingsNavigationItemSelected = true;
            else if (args.SelectedItem is MainViewModel.NavigationMenuItem item)
                Model.SelectedNavigationMenuItem = item;
        }
    }
}

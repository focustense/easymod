using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Profiles
{
    /// <summary>
    /// Interaction logic for NpcFilters.xaml
    /// </summary>
    public partial class NpcFilters : UserControl
    {
        protected NpcFiltersViewModel Model => (NpcFiltersViewModel)DataContext;

        public NpcFilters()
        {
            InitializeComponent();
        }

        private void ClearAvailablePluginFilterButton_Click(object sender, RoutedEventArgs e)
        {
            Model.AvailablePlugin = null;
        }

        private void ClearDefaultPluginFilterButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectedDefaultPlugin = null;
        }

        private void ClearFacePluginFilterButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectedFacePlugin = null;
        }

        private void PluginFiltersGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var menuItem = PluginFiltersGrid.FindVisualParentByType<MenuItem>();
            if (menuItem == null)
                return;
            menuItem.Focusable = false;
            menuItem.StaysOpenOnClick = true;
            menuItem.Style = FindResource("NonSelectableMenuItemStyle") as Style;
        }
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Profiles
{
    /// <summary>
    /// Interaction logic for NpcGrid.xaml
    /// </summary>
    public partial class NpcGrid : UserControl
    {
        public NpcGrid()
        {
            InitializeComponent();
        }

        private void NpcDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // I hate this code-behind hack, but the way ModernWpf defines its templates, plus some kind of apparent bug
            // in WPF itself, seems to make it impossible to do any other way. Using a combination of <Style> and
            // <Style.Resources>, it is entirely possible to target the ContentPresenter that is (for whatever reason)
            // set to explicitly align Left instead of using {TemplateBinding HorizontalContentAlignment} - but putting
            // a <Setter> on it to change the value of the presenter's HorizontalAlignment is simply ignored.
            // Every OTHER property can be changed by this style, it's just the one property we care about which can't.
            // But setting it programmatically (or via Snoop) is fine - only XAML has this problem.
            var columnHeaderContentPresenters = NpcDataGrid
                ?.FindVisualChildrenByName<Grid>("ColumnHeaderRoot")
                ?.Select(x => x.GetFirstVisualChildByType<Grid>())
                ?.Select(x => x?.GetFirstVisualChildByType<ContentPresenter>())
                ?.NotNull() ?? Enumerable.Empty<ContentPresenter>();
            foreach (var presenter in columnHeaderContentPresenters)
                presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void NpcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NpcDataGrid.SelectedItem != null)
                NpcDataGrid.ScrollIntoView(NpcDataGrid.SelectedItem);
        }
    }
}

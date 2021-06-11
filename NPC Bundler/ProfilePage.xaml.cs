using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TKey = Mutagen.Bethesda.FormKey;

namespace Focus.Apps.EasyNpc
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : ModernWpf.Controls.Page
    {
        protected ProfileViewModel<TKey> Model => ((MainViewModel)DataContext)?.Profile;

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void LoadProfile_Click(object sender, RoutedEventArgs e)
        {
            Model.LoadFromFile(Window.GetWindow(this));
        }

        private void MugshotListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            bool detectPlugin = !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl);
            var mugshot = (sender as FrameworkElement).DataContext as Mugshot;
            Model.SetFaceOverride(mugshot, detectPlugin);
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
                ?.Select(x => x.GetFirstVisualChildByType<ContentPresenter>());
            foreach (var presenter in columnHeaderContentPresenters)
                presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void NpcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NpcDataGrid.SelectedItem != null)
                NpcDataGrid.ScrollIntoView(NpcDataGrid.SelectedItem);
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            Model.SaveToFile(Window.GetWindow(this));
        }

        private void SetDefaultOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            var overrideConfig = ((FrameworkElement)e.Source).Tag as NpcOverrideConfiguration<TKey>;
            overrideConfig?.SetAsDefault();
        }

        private void SetFaceOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            var overrideConfig = ((FrameworkElement)e.Source).Tag as NpcOverrideConfiguration<TKey>;
            bool detectPlugin = !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl);
            overrideConfig?.SetAsFace(detectPlugin);
        }
    }
}

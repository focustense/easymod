using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#if MUTAGEN
using TKey = Mutagen.Bethesda.FormKey;
#else
using TKey = System.UInt32;
#endif

namespace NPC_Bundler
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

        private void MugshotListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OverrideListView.SelectedItem = null;
            Model.SelectMugshot(e.AddedItems.Cast<Mugshot>().FirstOrDefault());
        }

        private void NpcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model?.SelectNpc(e.AddedItems.Cast<NpcConfiguration<TKey>>().FirstOrDefault());
        }

        private void OverrideListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MugshotListView.SelectedItem = null;
            Model?.SelectOverride(e.AddedItems.Cast<NpcOverrideConfiguration<TKey>>().FirstOrDefault());
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

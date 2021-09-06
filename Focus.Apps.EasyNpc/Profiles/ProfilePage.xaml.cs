using System.Windows;
using System.Windows.Input;

namespace Focus.Apps.EasyNpc.Profiles
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : ModernWpf.Controls.Page
    {
        protected ProfileViewModel Model => ((IProfileContainer)DataContext)!.Profile;

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
            if ((sender as FrameworkElement)?.DataContext is MugshotViewModel mugshot)
                Model.SelectedNpc?.TrySetFaceMod(mugshot.ModName, out _);
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            Model.SaveToFile(Window.GetWindow(this));
        }

        private void SetDefaultOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).Tag is NpcOptionViewModel option)
                option.IsDefaultSource = true;
        }

        private void SetFaceOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).Tag is NpcOptionViewModel option)
                option.IsFaceSource = true;
        }
    }
}

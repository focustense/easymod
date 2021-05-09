using ModernWpf.Controls;
using System.Windows;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        protected SettingsViewModel Model => ((MainViewModel)DataContext).Settings;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void ModRootDirSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectModRootDirectory(Window.GetWindow(this));
        }

        private void MugshotsDirSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectMugshotsDirectory(Window.GetWindow(this));
        }
    }
}

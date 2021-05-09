using ModernWpf.Controls;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        protected ProfileViewModel Model => ((MainViewModel)DataContext)?.Profile;

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void NpcDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Model?.SelectNpc(NpcDataGrid.SelectedItem as NpcConfiguration);
        }
    }
}

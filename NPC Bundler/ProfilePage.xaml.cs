using System.Linq;
using System.Windows.Controls;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : ModernWpf.Controls.Page
    {
        protected ProfileViewModel Model => ((MainViewModel)DataContext)?.Profile;

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void MugshotListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OverrideListView.SelectedItem = null;
            Model.SelectMugshot(e.AddedItems.Cast<Mugshot>().FirstOrDefault());
        }

        private void NpcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model?.SelectNpc(e.AddedItems.Cast<NpcConfiguration>().FirstOrDefault());
        }

        private void OverrideListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MugshotListView.SelectedItem = null;
            Model?.SelectOverride(e.AddedItems.Cast<NpcOverrideConfiguration>().FirstOrDefault());
        }
    }
}

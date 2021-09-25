using System.Windows.Controls;
using System.Windows.Input;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    /// <summary>
    /// Interaction logic for PreBuildReportViewer.xaml
    /// </summary>
    public partial class BuildPreviewView : UserControl
    {
        protected BuildPreviewViewModel Model => (BuildPreviewViewModel)DataContext;

        public BuildPreviewView()
        {
            InitializeComponent();
        }

        private void NpcGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || sender is not DataGrid dg)
                return;
            if (dg.SelectedItem is IRecordKey key)
                Model.ShowProfile(key);
        }

        private void PluginGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || sender is not DataGrid dg)
                return;
            if (dg.SelectedItem is PluginViewModel plugin)
                Model.ShowMaster(plugin.PluginName);
        }
    }
}

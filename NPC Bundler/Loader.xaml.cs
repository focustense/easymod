using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for Loader.xaml
    /// </summary>
    public partial class Loader : UserControl
    {
        public Loader()
        {
            InitializeComponent();
        }

        protected LoaderViewModel Model => (LoaderViewModel)DataContext;

        private void ConfirmLoad_Click(object sender, RoutedEventArgs e)
        {
            Model.ConfirmPlugins();
        }

        private void LoadOrderGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var selectedPlugins = LoadOrderGrid.SelectedItems
                    .Cast<PluginSetting>()
                    .ToList();
                if (selectedPlugins.Count == 0)
                    return;
                var shouldLoad = selectedPlugins.Any(x => !x.ShouldLoad);
                foreach (var plugin in selectedPlugins)
                    plugin.ShouldLoad = shouldLoad;
            }
        }
    }
}

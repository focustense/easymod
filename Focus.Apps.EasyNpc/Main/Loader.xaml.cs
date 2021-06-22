using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TKey = Mutagen.Bethesda.Plugins.FormKey;

namespace Focus.Apps.EasyNpc.Main
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

        protected LoaderViewModel<TKey> Model => (LoaderViewModel<TKey>)DataContext;

        private void ConfirmLoad_Click(object sender, RoutedEventArgs e)
        {
            Model.ConfirmPlugins();
        }

        private void LoadOrderGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                Model.TogglePlugins(LoadOrderGrid.SelectedItems.Cast<PluginSetting>());
            }
        }
    }
}

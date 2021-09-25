using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

        private void NpcsListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ScrollToGroup(NpcsExpander);
        }

        private void PluginGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || sender is not DataGrid dg)
                return;
            if (dg.SelectedItem is PluginViewModel plugin)
                Model.ShowMaster(plugin.PluginName);
        }

        private void PluginsListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ScrollToGroup(PluginsExpander);
        }

        private void ScrollToGroup(Expander exp)
        {
            exp.IsExpanded = true;
            var originPoint = exp.TranslatePoint(new Point(), ContentScrollViewer);
            var newOffset = originPoint.Y + ContentScrollViewer.VerticalOffset;
            if (newOffset != ContentScrollViewer.VerticalOffset)
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    ScrollAnimation.AnimateVertical(ContentScrollViewer, newOffset);
                }));
            
        }

        private void SummaryListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (ContentScrollViewer.VerticalOffset != 0)
                ScrollAnimation.AnimateVertical(ContentScrollViewer, 0);
        }
    }
}

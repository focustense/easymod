using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private CancellationTokenSource? scrollCts;
        private bool suppressSectionSelections = false;

        public BuildPreviewView()
        {
            InitializeComponent();
        }

        private void ActivateSection(FrameworkElement navigationElement)
        {
            var expander = navigationElement.Tag as Expander;
            if (expander is not null)
                ScrollToGroup(expander);
            else
                ScrollToOffset(0);
        }

        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is null)
                return;
            // For whatever reason, WPF completely refuses to update the attached property from ScrollAnimation as a
            // data binding, so we have to force it like this.
            // Also, we read the scroll position directly from the control instead of taking it from the event, because
            // scroll events fire chaotically and in a bizarre order.
            Model.ScrollPosition = ContentScrollViewer.VerticalOffset;

            if (scrollCts is not null)
                return;

            suppressSectionSelections = true;
            try
            {
                if (ContentScrollViewer.VerticalOffset == 0)
                {
                    SectionsListBox.SelectedIndex = 0;
                    return;
                }
                var items = SectionsListBox.Items.Cast<ListBoxItem>();
                var expanders = items.Select(x => x.Tag).OfType<Expander>().Where(x => x.IsExpanded);
                var bestExpander = expanders.FirstOrDefault(IsFullyVisible) ?? expanders.FirstOrDefault(IsTopVisible);
                if (bestExpander is not null)
                    SectionsListBox.SelectedItem = items.FirstOrDefault(x => x.Tag == bestExpander);
                else
                    SectionsListBox.SelectedIndex = 0;
            }
            finally
            {
                suppressSectionSelections = false;
            }
        }

        private bool IsFullyVisible(FrameworkElement element)
        {
            var elementTop = element.TranslatePoint(new(), ContentScrollViewer).Y;
            var elementBottom = element.TranslatePoint(new(0, element.ActualHeight), ContentScrollViewer).Y;
            return
                elementTop >= 0 && elementTop < ContentScrollViewer.ScrollableHeight &&
                elementBottom >= 0 && elementBottom < ContentScrollViewer.ScrollableHeight;
        }

        private bool IsTopVisible(FrameworkElement element)
        {
            var elementTop = element.TranslatePoint(new(), ContentScrollViewer).Y;
            return elementTop >= 0 && elementTop < ContentScrollViewer.ScrollableHeight;
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

        private void ScrollToGroup(Expander exp)
        {
            exp.IsExpanded = true;
            var originPoint = exp.TranslatePoint(new Point(), ContentScrollViewer);
            var newOffset = originPoint.Y + ContentScrollViewer.VerticalOffset;
            ScrollToOffset(newOffset);
        }

        private void ScrollToOffset(double offset)
        {
            if (offset == ContentScrollViewer.VerticalOffset)
                return;
            scrollCts?.Cancel();
            var currentCts = scrollCts = new();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(async () =>
            {
                try
                {
                    await ScrollAnimation.AnimateVertical(ContentScrollViewer, offset, scrollCts.Token);
                }
                catch (TaskCanceledException) { }
                finally
                {
                    currentCts.Dispose();
                    if (scrollCts == currentCts)
                        scrollCts = null;
                }
            }));
        }

        private void SectionListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!suppressSectionSelections && sender is FrameworkElement fe)
                ActivateSection(fe);
        }

        private void SectionListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (!suppressSectionSelections && sender is FrameworkElement fe)
                ActivateSection(fe);
        }

        private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is not null)
                ContentScrollViewer.ScrollToVerticalOffset(Model.ScrollPosition);
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not null)
                ContentScrollViewer.ScrollToVerticalOffset(Model.ScrollPosition);
        }

        private void WarningsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            if (WarningsListBox.SelectedItem is BuildWarning buildWarning && buildWarning.RecordKey != null)
                Model.ExpandWarning(buildWarning);
        }
    }
}

using Bindables;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Reports
{
    /// <summary>
    /// Interaction logic for SummaryGroup.xaml
    /// </summary>
    [DependencyProperty]
    public partial class SummaryGroup : UserControl
    {
        public string Title { get; set; }
        public ObservableCollection<SummaryItem> Items { get; set; }

        public SummaryGroup()
        {
            InitializeComponent();
        }
    }

    public class SummaryGroupDesignContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Title { get; set; }
        public IEnumerable<SummaryItem> Items { get; set; }
    }
}

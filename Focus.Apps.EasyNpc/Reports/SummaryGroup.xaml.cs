using PropertyChanged;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Focus.Apps.EasyNpc.Reports
{
    /// <summary>
    /// Interaction logic for SummaryGroup.xaml
    /// </summary>
    public partial class SummaryGroup : UserControl
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items", typeof(IEnumerable<SummaryItem>), typeof(SummaryGroup),
            new PropertyMetadata(Enumerable.Empty<SummaryItem>()));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(SummaryGroup), new PropertyMetadata(""));

        public IEnumerable<SummaryItem> Items
        {
            get { return (IEnumerable<SummaryItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public SummaryGroup()
        {
            InitializeComponent();
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class SummaryGroupDesignContext
    {
        public string Title { get; set; } = string.Empty;
        public IEnumerable<SummaryItem> Items { get; set; } = Enumerable.Empty<SummaryItem>();
    }
}

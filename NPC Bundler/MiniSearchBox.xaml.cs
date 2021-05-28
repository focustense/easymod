using System;
using System.Windows;
using System.Windows.Controls;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for MiniSearchBox.xaml
    /// </summary>
    public partial class MiniSearchBox : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(MiniSearchBox), new PropertyMetadata(string.Empty));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public MiniSearchBox()
        {
            InitializeComponent();
        }
    }
}

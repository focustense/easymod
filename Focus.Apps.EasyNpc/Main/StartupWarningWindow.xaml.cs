using System;
using System.Windows;

namespace Focus.Apps.EasyNpc.Main
{
    /// <summary>
    /// Interaction logic for StartupWarningWindow.xaml
    /// </summary>
    public partial class StartupWarningWindow : Window
    {
        public StartupWarningWindow()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}

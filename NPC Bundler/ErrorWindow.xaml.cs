using System;
using System.Windows;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        protected CrashViewModel Model => (CrashViewModel)DataContext;

        public ErrorWindow()
        {
            InitializeComponent();
        }

        private void LogDirectoryLink_Click(object sender, RoutedEventArgs e)
        {
            Model.OpenLogDirectory();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

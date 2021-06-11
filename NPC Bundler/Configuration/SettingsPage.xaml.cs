using System.Windows;

namespace Focus.Apps.EasyNpc.Configuration
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : ModernWpf.Controls.Page
    {
        protected SettingsViewModel Model => ((ISettingsContainer)DataContext).Settings;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void AddBuildWarningSuppressionButton_Click(object sender, RoutedEventArgs e)
        {
            Model.AddBuildWarningSuppression();
        }

        private void BuildWarningsComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            // WPF's ComboBox makes a goddamn mess of the UI if any user is unlucky enough to click outside of the
            // checkbox - i.e. within the ComboBoxItem's padding - and there's no other way to pad around the checkbox
            // itself. One lame but somewhat effective workaround is just to force the grid out of editing mode when
            // this happens, which makes the ComboBox disappear entirely and get replaced by the regular text which
            // renders fine.
            BuildWarningWhitelistGrid.CancelEdit();
        }

        private void ModRootDirSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectModRootDirectory(Window.GetWindow(this));
        }

        private void MugshotsDirSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectMugshotsDirectory(Window.GetWindow(this));
        }

        private void RemoveWarningSuppressionsButton_Click(object sender, RoutedEventArgs e)
        {
            var suppressions = (sender as FrameworkElement).DataContext as BuildWarningSuppressions;
            Model.RemoveBuildWarningSuppression(suppressions);
        }

        private void WelcomeDoneButton_Click(object sender, RoutedEventArgs e)
        {
            Model.AckWelcome();
        }
    }
}

using System;
using System.Windows;

using TKey = Mutagen.Bethesda.FormKey;

namespace Focus.Apps.EasyNpc.Build
{
    /// <summary>
    /// Interaction logic for BuildPage.xaml
    /// </summary>
    public partial class BuildPage : ModernWpf.Controls.Page
    {
        protected BuildViewModel<TKey> Model => ((IBuildContainer<TKey>)DataContext)?.Build;

        public BuildPage()
        {
            InitializeComponent();
        }

        private void AltCheckForProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.CheckForProblems();
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            Model.BeginBuild();
        }

        private void CheckForProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.CheckForProblems();
        }

        private void DismissProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.DismissProblems();
        }

        private void DoneNoProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.DismissProblems();
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Model.OpenBuildOutput();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Model?.QuickRefresh();
        }

        private void SkipProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.IsProblemCheckerVisible = false;
        }
    }
}

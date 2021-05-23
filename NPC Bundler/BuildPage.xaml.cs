﻿using System;
using System.Windows;

using TKey = System.UInt32;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for BuildPage.xaml
    /// </summary>
    public partial class BuildPage : ModernWpf.Controls.Page
    {
        protected BuildViewModel<TKey> Model => ((MainViewModel)DataContext)?.Build;

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

        private void SkipProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.IsProblemCheckerVisible = false;
        }
    }
}

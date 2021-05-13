﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : ModernWpf.Controls.Page
    {
        protected ProfileViewModel Model => ((MainViewModel)DataContext)?.Profile;

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void MugshotListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            bool detectPlugin = !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl);
            var mugshot = (sender as FrameworkElement).DataContext as Mugshot;
            Model.SetFaceOverride(mugshot, detectPlugin);
        }

        private void MugshotListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OverrideListView.SelectedItem = null;
            Model.SelectMugshot(e.AddedItems.Cast<Mugshot>().FirstOrDefault());
        }

        private void NpcDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model?.SelectNpc(e.AddedItems.Cast<NpcConfiguration>().FirstOrDefault());
        }

        private void OverrideListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MugshotListView.SelectedItem = null;
            Model?.SelectOverride(e.AddedItems.Cast<NpcOverrideConfiguration>().FirstOrDefault());
        }

        private void SetDefaultOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            var overrideConfig = ((FrameworkElement)e.Source).Tag as NpcOverrideConfiguration;
            overrideConfig?.SetAsDefault();
        }

        private void SetFaceOverrideButton_Click(object sender, RoutedEventArgs e)
        {
            var overrideConfig = ((FrameworkElement)e.Source).Tag as NpcOverrideConfiguration;
            bool detectPlugin = !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl);
            overrideConfig?.SetAsFace(detectPlugin);
        }
    }
}

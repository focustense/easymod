﻿using System;
using System.Threading.Tasks;
using System.Windows;

using TKey = Mutagen.Bethesda.FormKey;

namespace NPC_Bundler
{
    /// <summary>
    /// Interaction logic for MaintenancePage.xaml
    /// </summary>
    public partial class MaintenancePage : ModernWpf.Controls.Page
    {
        protected MaintenanceViewModel<TKey> Model => ((MainViewModel)DataContext)?.Maintenance;

        public MaintenancePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Model?.Refresh();
        }

        private void DeleteLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var model = Model;
            Task.Run(() => model?.DeleteOldLogFiles());
        }

        private void EvolveToLoadOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var model = Model;
            Task.Run(() => model?.ResetNpcDefaults());
        }

        private void TrimAutosaveButton_Click(object sender, RoutedEventArgs e)
        {
            var model = Model;
            Task.Run(() => model?.TrimAutoSave());
        }
    }
}

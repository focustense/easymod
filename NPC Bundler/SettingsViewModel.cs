using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NPC_Bundler
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly BundlerSettings Settings = BundlerSettings.Default;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ModRootDirectory
        {
            get => Settings.ModRootDirectory;
            set
            {
                Settings.ModRootDirectory = value;
                Settings.Save();
            }
        }

        public string MugshotsDirectory
        {
            get => Settings.MugshotsDirectory;
            set
            {
                Settings.MugshotsDirectory = value;
                Settings.Save();
            }
        }

        public void SelectModRootDirectory(Window owner)
        {
            const string description = "Select root directory where your individual mod directories are located";
            if (SelectDirectory(owner, description, out string modRootDirectory))
                ModRootDirectory = modRootDirectory;

        }

        public void SelectMugshotsDirectory(Window owner)
        {
            const string description = "Select directory containing face previews";
            if (SelectDirectory(owner, description, out string mugshotsDirectory))
                MugshotsDirectory = mugshotsDirectory;
        }

        private bool SelectDirectory(Window owner, string description, out string selectedPath)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = description;
            dialog.UseDescriptionForTitle = true;
            bool result = dialog.ShowDialog(owner).GetValueOrDefault();
            if (result)
                selectedPath = dialog.SelectedPath;
            else
                selectedPath = string.Empty;
            return result;
        }
    }
}

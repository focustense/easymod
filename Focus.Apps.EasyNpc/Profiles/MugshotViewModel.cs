#nullable enable

using System.Collections.Generic;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class MugshotViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<string> InstalledPlugins => mugshot.InstalledPlugins;
        public bool IsFocused { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsModInstalled => mugshot.InstalledMod is not null;
        public bool IsPluginLoaded => mugshot.InstalledPlugins.Count > 0;
        public bool IsSelectedSource { get; set; }
        public string ModName => mugshot.ModName;
        public string Path => mugshot.Path;

        private readonly MugshotModel mugshot;

        public MugshotViewModel(MugshotModel mugshot, bool isSelectedSource = false)
        {
            this.mugshot = mugshot;
            IsSelectedSource = isSelectedSource;
        }
    }
}

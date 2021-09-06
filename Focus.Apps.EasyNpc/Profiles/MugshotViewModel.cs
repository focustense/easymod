using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    [AddINotifyPropertyChangedInterface]
    public class MugshotViewModel
    {
        public IReadOnlyList<string> InstalledPlugins => mugshot.InstalledPlugins;
        public bool IsDisabledByErrors { get; private init; }
        public bool IsFocused { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsModDisabled => IsModInstalled && !mugshot.InstalledComponents.Any(x => x.IsEnabled);
        public bool IsModInstalled => mugshot.InstalledMod is not null;
        public bool IsPluginLoaded => mugshot.InstalledPlugins.Count > 0;
        public bool IsSelectedSource { get; set; }
        public string ModName => mugshot.ModName;
        public string Path => mugshot.Path;

        private readonly Mugshot mugshot;

        public MugshotViewModel(Mugshot mugshot, IEnumerable<NpcOption> options, bool isSelectedSource = false)
        {
            this.mugshot = mugshot;
            IsSelectedSource = isSelectedSource;

            var applicableOptions = options
                .Where(x => mugshot.InstalledPlugins.Contains(x.PluginName, StringComparer.CurrentCultureIgnoreCase))
                .ToList();
            if (applicableOptions.Count > 0 && applicableOptions.All(x => x.HasErrors))
                IsDisabledByErrors = true;
        }
    }
}

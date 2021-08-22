using Focus.ModManagers;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class Mugshot
    {
        public ModInfo? InstalledMod { get; init; }
        public IReadOnlyList<string> InstalledPlugins { get; init; } = new List<string>().AsReadOnly();
        public bool IsPlaceholder { get; init; }
        public string ModName { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
    }
}

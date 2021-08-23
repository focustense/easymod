using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.ModManagers
{
    public class ModInfo : IModLocatorKey
    {
        public static readonly IEqualityComparer<ModInfo> KeyComparer = new ModInfoByKeyComparer();

        public IEnumerable<string> AllNames => Components.Select(x => x.Name).Prepend(Name);

        public IReadOnlyList<ModComponentInfo> Components { get; init; }
        public string Id { get; init; }
        public string Name { get; init; }

        public ModInfo(string id, string name, IEnumerable<ModComponentInfo>? components = null)
        {
            Id = id;
            Name = name;
            Components = (components ?? Enumerable.Empty<ModComponentInfo>()).ToList().AsReadOnly();
        }

        public bool IncludesName(string name)
        {
            return AllNames.Any(x => x.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    public class ModComponentInfo
    {
        public IModLocatorKey ModKey { get; init; }
        public string Id { get; init; }
        public bool IsEnabled { get; init; }
        public string Name { get; init; }
        public string Path { get; init; }

        public ModComponentInfo(IModLocatorKey modKey, string id, string name, string path, bool isEnabled = true)
        {
            ModKey = modKey;
            Id = id;
            Name = name;
            Path = path;
            IsEnabled = isEnabled;
        }
    }

    class ModInfoByKeyComparer : IEqualityComparer<ModInfo>
    {
        public bool Equals(ModInfo? x, ModInfo? y)
        {
            return ModLocatorKeyComparer.Default.Equals(x, y);
        }

        public int GetHashCode(ModInfo obj)
        {
            return obj is not null ? ModLocatorKeyComparer.Default.GetHashCode(obj) : 0;
        }
    }
}

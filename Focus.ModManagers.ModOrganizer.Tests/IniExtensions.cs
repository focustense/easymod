using IniParser.Model;
using System.Collections.Generic;

namespace Focus.ModManagers.ModOrganizer.Tests
{
    static class IniExtensions
    {
        public static void AddSection(this IniData ini, string keyName, Dictionary<string, string> data)
        {
            ini.Sections.AddSection(keyName);
            var section = ini.Sections[keyName];
            foreach (var item in data)
                section.AddKey(item.Key, item.Value);
        }
    }
}

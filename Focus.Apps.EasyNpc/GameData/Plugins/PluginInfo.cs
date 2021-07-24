using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.GameData.Plugins
{
    public class PluginInfo
    {
        public string FileName { get; init; }
        public bool IsEnabled { get; init; }
        public bool IsReadable { get; init; }
        public IReadOnlyList<string> Masters { get; init; }

        public PluginInfo()
        {
        }

        public PluginInfo(string fileName, IEnumerable<string> masters, bool isReadable, bool isEnabled)
        {
            FileName = fileName;
            Masters = masters.ToList().AsReadOnly();
            IsReadable = isReadable;
            IsEnabled = isEnabled;
        }
    }
}
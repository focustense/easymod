using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Environment
{
    public class PluginInfo
    {
        public string FileName { get; init; }
        public bool IsEnabled { get; init; }
        public bool IsImplicit { get; init; }
        public bool IsReadable { get; init; }
        public IReadOnlyList<string> Masters { get; init; }

        public PluginInfo()
            : this("", Enumerable.Empty<string>(), false, false)
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
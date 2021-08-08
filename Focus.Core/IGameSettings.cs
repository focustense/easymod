using System;
using System.Collections.Generic;
using System.IO;

namespace Focus
{
    public interface IGameSettings
    {
        IEnumerable<string> ArchiveOrder { get; }
        string DataDirectory { get; }
        IEnumerable<string> PluginLoadOrder { get; }
    }
}

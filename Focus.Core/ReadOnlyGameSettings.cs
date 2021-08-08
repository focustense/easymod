using System.Collections.Generic;
using System.Linq;

namespace Focus
{
    public class ReadOnlyGameSettings : IGameSettings
    {
        public IEnumerable<string> ArchiveOrder { get; init; } = Enumerable.Empty<string>();
        public string DataDirectory { get; init; } = "";
        public IEnumerable<string> PluginLoadOrder { get; init; } = Enumerable.Empty<string>();

        public ReadOnlyGameSettings(
            string dataDirectory = "", IEnumerable<string>? pluginLoadOrder = null,
            IEnumerable<string>? archiveOrder = null)
        {
            DataDirectory = dataDirectory;
            PluginLoadOrder = pluginLoadOrder ?? Enumerable.Empty<string>();
            ArchiveOrder = archiveOrder ?? Enumerable.Empty<string>();
        }
    }
}

using System.Collections.Generic;

namespace Focus
{
    public interface IGameSettings
    {
        IEnumerable<string> ArchiveOrder { get; }
        string DataDirectory { get; }
        IEnumerable<string> PluginLoadOrder { get; }

        bool IsBaseGameArchive(string archiveName);
    }
}

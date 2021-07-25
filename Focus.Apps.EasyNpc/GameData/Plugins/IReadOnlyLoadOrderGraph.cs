using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.GameData.Plugins
{
    public interface IReadOnlyLoadOrderGraph
    {
        bool CanLoad(string pluginName);
        IEnumerable<string> GetAllMasters(string pluginName);
        IEnumerable<string> GetMissingMasters(string pluginName);
        bool IsEnabled(string pluginName);
    }
}
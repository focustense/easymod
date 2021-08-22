using System.Collections.Generic;

namespace Focus.Environment
{
    public interface IReadOnlyLoadOrderGraph
    {
        bool CanLoad(string pluginName);
        IEnumerable<string> GetAllMasters(string pluginName, bool includeImplicit = false);
        IEnumerable<string> GetMissingMasters(string pluginName);
        bool IsEnabled(string pluginName);
        bool IsImplicit(string pluginName);
    }
}
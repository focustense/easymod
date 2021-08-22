using System.Collections.Generic;
using System.Linq;

namespace Focus.Environment
{
    public interface IGameSetup
    {
        IReadOnlyList<PluginInfo> AvailablePlugins { get; }
        string DataDirectory { get; }
        ILoadOrderGraph LoadOrderGraph { get; }
    }

    public static class GameSetupExtensions
    {
        public static IEnumerable<string> GetBaseGamePlugins(this IGameSetup setup)
        {
            return setup.AvailablePlugins
                .Select(p => p.FileName)
                .Where(f => setup.LoadOrderGraph.IsImplicit(f));
        }
    }
}

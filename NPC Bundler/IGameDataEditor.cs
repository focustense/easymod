using System.Collections.Generic;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public interface IGameDataEditor<TKey>
        where TKey : struct
    {
        IModPluginMapFactory ModPluginMapFactory { get; }

        IEnumerable<string> GetAvailablePlugins();
        IEnumerable<string> GetLoadedPlugins();
        int GetLoadOrderIndex(string pluginName);
        bool IsMaster(string pluginName);
        Task Load(IEnumerable<string> pluginNames);
        void ReadNpcRecords(string pluginName, IDictionary<TKey, IMutableNpc<TKey>> cache);
    }
}
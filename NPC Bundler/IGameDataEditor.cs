using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public interface IGameDataEditor<TKey>
        where TKey : struct
    {
        IArchiveProvider ArchiveProvider { get; }
        IExternalLog Log { get; }
        IMergedPluginBuilder<TKey> MergedPluginBuilder { get; }
        IModPluginMapFactory ModPluginMapFactory { get; }

        IEnumerable<Tuple<string, bool>> GetAvailablePlugins();
        IEnumerable<string> GetLoadedPlugins();
        int GetLoadOrderIndex(string pluginName);
        bool IsMaster(string pluginName);
        Task Load(IEnumerable<string> pluginNames);
        void ReadNpcRecords(string pluginName, IDictionary<TKey, IMutableNpc<TKey>> cache);
    }
}
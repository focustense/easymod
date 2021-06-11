﻿using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc
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
        IEnumerable<Hair<TKey>> ReadHairRecords(string pluginName);
        void ReadNpcRecords(string pluginName, IDictionary<TKey, IMutableNpc<TKey>> cache);
    }
}
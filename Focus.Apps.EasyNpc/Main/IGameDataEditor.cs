﻿using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using Focus.Environment;
using Focus.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Main
{
    public interface IGameDataEditor<TKey>
        where TKey : struct
    {
        IArchiveProvider ArchiveProvider { get; }
        string DataDirectory { get; }
        IExternalLog Log { get; }
        IMergedPluginBuilder<TKey> MergedPluginBuilder { get; }
        IModPluginMapFactory ModPluginMapFactory { get; }
        IGameSettings Settings { get; }

        IEnumerable<PluginInfo> GetAvailablePlugins();
        IEnumerable<string> GetLoadedPlugins();
        int GetLoadOrderIndex(string pluginName);
        bool IsMaster(string pluginName);
        Task Load(IEnumerable<string> pluginNames);
        IEnumerable<Hair<TKey>> ReadHairRecords(string pluginName);
        void ReadNpcRecords(string pluginName, IDictionary<TKey, IMutableNpc<TKey>> cache);
    }
}
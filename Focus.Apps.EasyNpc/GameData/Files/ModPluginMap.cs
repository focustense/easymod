using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.GameData.Files
{
    public class ModPluginMap
    {
        private static readonly ModPluginMap Empty = new(
            EmptyDictionary<string, string>(), EmptyDictionary<string, string>(),
            EmptyDictionary<string, string>(), EmptyDictionary<string, string>());
        private static readonly Dictionary<string, ModPluginMap> Instances = new();

        enum FileType { Unknown, Plugin, Archive };

        public static ModPluginMap ForDirectory(
            string modRootDirectory, IModResolver modResolver, IEnumerable<string> pluginNames,
            IEnumerable<string> archiveNames)
        {
            var trimmedDirectory = Path.TrimEndingDirectorySeparator(modRootDirectory);
            if (string.IsNullOrEmpty(trimmedDirectory) || !Directory.Exists(trimmedDirectory))
                return Empty;
            if (Instances.TryGetValue(trimmedDirectory, out ModPluginMap cached))
                return cached;

            // There's probably not a lot we can do about this O(N^2)ish mess; the problem is a many-to-many
            // relationship between mods and plugins, a mod can contain many plugins and the same plugin can be included
            // in many mods (i.e. overriding the ESP in MO). We CAN get priority information from a laborious process
            // of reading Mod Organizer INIs, but that turns our "soft" MO dependency into a "hard" one and can still
            // make mistakes, i.e. if the bottom-most mod providing a plugin is not the same one providing facegendata.
            // This actually happens, such as with "AIO" plugins created to merge individual replacers that still require
            // the loose files from the original mods.
            // So, this is a little slow, but still probably the best way.
            var archiveFileNames = new HashSet<string>(archiveNames, StringComparer.OrdinalIgnoreCase);
            var pluginFileNames = new HashSet<string>(pluginNames, StringComparer.OrdinalIgnoreCase);
            var modsWithPlugins = Directory.EnumerateDirectories(trimmedDirectory)
                .Select(modDir => new
                {
                    ModName = modResolver.GetModName(modDir),
                    Files = Directory.EnumerateFiles(modDir)
                        .Select(path => Path.GetFileName(path))
                        .Select(fileName => new
                        {
                            FileName = fileName,
                            FileType =
                                pluginFileNames.Contains(fileName) ? FileType.Plugin :
                                archiveFileNames.Contains(fileName) ? FileType.Archive :
                                FileType.Unknown,
                        })
                        .Where(f => f.FileType != FileType.Unknown)
                })
                .GroupBy(x => x.ModName)
                .Select(g => new {
                    Mod = g.Key,
                    Archives = g.SelectMany(x => x.Files)
                        .Where(f => f.FileType == FileType.Archive)
                        .Select(f => f.FileName)
                        .ToArray(),
                    Plugins = g.SelectMany(x => x.Files)
                        .Where(f => f.FileType == FileType.Plugin)
                        .Select(f => f.FileName)
                        .ToArray(),
                })
                .ToList();
            IDictionary<string, IEnumerable<string>> modsToPlugins = null;
            IDictionary<string, IEnumerable<string>> pluginsToMods = null;
            IDictionary<string, IEnumerable<string>> modsToArchives= null;
            IDictionary<string, IEnumerable<string>> archivesToMods = null;
            Parallel.Invoke(
                () =>
                {
                    modsToArchives = modsWithPlugins.ToDictionary(
                        x => x.Mod,
                        x => x.Archives.ToList().AsReadOnly().AsEnumerable(),
                        StringComparer.OrdinalIgnoreCase);
                },
                () =>
                {
                    archivesToMods = modsWithPlugins
                        .SelectMany(x => x.Archives.Select(a => new { x.Mod, Archive = a }))
                        .GroupBy(x => x.Archive, x => x.Mod, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.ToList().AsReadOnly().AsEnumerable(),
                            StringComparer.OrdinalIgnoreCase);
                    foreach (var archive in archiveFileNames)
                        archivesToMods.TryAdd(archive, Enumerable.Empty<string>());
                },
                () => {
                    modsToPlugins = modsWithPlugins.ToDictionary(
                        x => x.Mod,
                        x => x.Plugins.ToList().AsReadOnly().AsEnumerable(),
                        StringComparer.OrdinalIgnoreCase);
                },
                () => {
                    pluginsToMods = modsWithPlugins
                        .SelectMany(x => x.Plugins.Select(p => new { x.Mod, Plugin = p }))
                        .GroupBy(x => x.Plugin, x => x.Mod, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.ToList().AsReadOnly().AsEnumerable(),
                            StringComparer.OrdinalIgnoreCase);
                    foreach (var plugin in pluginFileNames)
                        pluginsToMods.TryAdd(plugin, Enumerable.Empty<string>());
                });
            var map = new ModPluginMap(
                new ReadOnlyDictionary<string, IEnumerable<string>>(modsToPlugins),
                new ReadOnlyDictionary<string, IEnumerable<string>>(pluginsToMods),
                new ReadOnlyDictionary<string, IEnumerable<string>>(modsToArchives),
                new ReadOnlyDictionary<string, IEnumerable<string>>(archivesToMods));
            Instances.TryAdd(modRootDirectory, map);
            return map;
        }

        private readonly IReadOnlyDictionary<string, IEnumerable<string>> archivesToMods;
        private readonly IReadOnlyDictionary<string, IEnumerable<string>> modsToArchives;
        private readonly IReadOnlyDictionary<string, IEnumerable<string>> modsToPlugins;
        private readonly IReadOnlyDictionary<string, IEnumerable<string>> pluginsToMods;

        private ModPluginMap(
            IReadOnlyDictionary<string, IEnumerable<string>> modsToPlugins,
            IReadOnlyDictionary<string, IEnumerable<string>> pluginsToMods,
            IReadOnlyDictionary<string, IEnumerable<string>> modsToArchives,
            IReadOnlyDictionary<string, IEnumerable<string>> archivesToMods)
        {
            this.modsToPlugins = modsToPlugins;
            this.pluginsToMods = pluginsToMods;
            this.modsToArchives = modsToArchives;
            this.archivesToMods = archivesToMods;
        }

        public IEnumerable<string> GetArchivesForMod(string modName)
        {
            return modsToArchives.TryGetValue(modName, out IEnumerable<string> archives) ?
                archives : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetModsForArchive(string archiveName)
        {
            return archivesToMods.TryGetValue(archiveName, out IEnumerable<string> mods) ?
                mods : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetModsForPlugin(string pluginName)
        {
            return pluginsToMods.TryGetValue(pluginName, out IEnumerable<string> mods) ?
                mods : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetPluginsForMod(string modName)
        {
            return modsToPlugins.TryGetValue(modName, out IEnumerable<string> plugins) ?
                plugins : Enumerable.Empty<string>();
        }

        public bool IsModInstalled(string modName)
        {
            return modsToPlugins.ContainsKey(modName);
        }

        private static IReadOnlyDictionary<TKey, IEnumerable<TValue>> EmptyDictionary<TKey, TValue>()
        {
            return new ReadOnlyDictionary<TKey, IEnumerable<TValue>>(new Dictionary<TKey, IEnumerable<TValue>>());
        }
    }
}

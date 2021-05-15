using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    class ModPluginMap
    {
        private static readonly Dictionary<string, ModPluginMap> Instances = new();

        enum FileType { Unknown, Plugin, Archive };

        public static ModPluginMap ForDirectory(string modRootDirectory)
        {
            var trimmedDirectory = Path.TrimEndingDirectorySeparator(modRootDirectory);
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
            var archiveFileNames = new HashSet<string>(
                Resources.GetLoadedContainers().Select(f => Path.GetFileName(f)),
                StringComparer.OrdinalIgnoreCase);
            var pluginFileNames = new HashSet<string>(Setup.GetLoadedFileNames(), StringComparer.OrdinalIgnoreCase);
            var modsWithPlugins = Directory.EnumerateDirectories(trimmedDirectory)
                .Select(modDir => new
                {
                    Mod = Path.GetFileName(Path.TrimEndingDirectorySeparator(modDir)),
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
                .Select(x => new {
                    x.Mod,
                    Archives = x.Files.Where(f => f.FileType == FileType.Archive).Select(f => f.FileName).ToArray(),
                    Plugins = x.Files.Where(f => f.FileType == FileType.Plugin).Select(f => f.FileName).ToArray(),
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
                        x => x.Archives.ToList().AsReadOnly().AsEnumerable());
                },
                () =>
                {
                    archivesToMods = modsWithPlugins
                        .SelectMany(x => x.Archives.Select(a => new { x.Mod, Archive = a }))
                        .GroupBy(x => x.Archive, x => x.Mod)
                        .ToDictionary(x => x.Key, x => x.ToList().AsReadOnly().AsEnumerable());
                    foreach (var archive in archiveFileNames)
                        archivesToMods.TryAdd(archive, Enumerable.Empty<string>());
                },
                () => {
                    modsToPlugins = modsWithPlugins.ToDictionary(
                        x => x.Mod,
                        x => x.Plugins.ToList().AsReadOnly().AsEnumerable());
                },
                () => {
                    pluginsToMods = modsWithPlugins
                        .SelectMany(x => x.Plugins.Select(p => new { x.Mod, Plugin = p }))
                        .GroupBy(x => x.Plugin, x => x.Mod)
                        .ToDictionary(x => x.Key, x => x.ToList().AsReadOnly().AsEnumerable());
                    foreach (var plugin in pluginFileNames)
                        pluginsToMods.TryAdd(plugin, Enumerable.Empty<string>());
                });
            var map = new ModPluginMap(modsToPlugins, pluginsToMods, modsToArchives, archivesToMods);
            Instances.TryAdd(modRootDirectory, map);
            return map;
        }

        private readonly IDictionary<string, IEnumerable<string>> archivesToMods;
        private readonly IDictionary<string, IEnumerable<string>> modsToArchives;
        private readonly IDictionary<string, IEnumerable<string>> modsToPlugins;
        private readonly IDictionary<string, IEnumerable<string>> pluginsToMods;

        private ModPluginMap(
            IDictionary<string, IEnumerable<string>> modsToPlugins,
            IDictionary<string, IEnumerable<string>> pluginsToMods,
            IDictionary<string, IEnumerable<string>> modsToArchives,
            IDictionary<string, IEnumerable<string>> archivesToMods)
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
    }
}

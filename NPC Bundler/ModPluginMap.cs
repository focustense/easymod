using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    class ModPluginMap
    {
        private static readonly Dictionary<string, ModPluginMap> Instances = new();

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
            var pluginFileNames = new HashSet<string>(Setup.GetLoadedFileNames(), StringComparer.OrdinalIgnoreCase);
            var modsWithPlugins = Directory.EnumerateDirectories(trimmedDirectory)
                .Select(modDir => new {
                    Mod = Path.GetFileName(Path.TrimEndingDirectorySeparator(modDir)),
                    Plugins = Directory.EnumerateFiles(modDir)
                        .Where(f => pluginFileNames.Contains(Path.GetFileName(f)))
                })
                .ToList();
            IDictionary<string, IEnumerable<string>> modsToPlugins = null;
            IDictionary<string, IEnumerable<string>> pluginsToMods = null;
            Parallel.Invoke(
                () => {
                    modsToPlugins = modsWithPlugins.ToDictionary(
                        x => x.Mod,
                        x => x.Plugins.ToList().AsReadOnly().AsEnumerable());
                },
                () => {
                    pluginsToMods = modsWithPlugins
                        .SelectMany(x => x.Plugins.Select(p => new { Mod = x.Mod, Plugin = p }))
                        .GroupBy(x => x.Plugin, x => x.Mod)
                        .ToDictionary(x => x.Key, x => x.ToList().AsReadOnly().AsEnumerable());
                    foreach (var plugin in pluginFileNames)
                        pluginsToMods.TryAdd(plugin, Enumerable.Empty<string>());
                });
            var map = new ModPluginMap(modsToPlugins, pluginsToMods);
            Instances.TryAdd(modRootDirectory, map);
            return map;
        }

        private readonly IDictionary<string, IEnumerable<string>> modsToPlugins;
        private readonly IDictionary<string, IEnumerable<string>> pluginsToMods;

        private ModPluginMap(
            IDictionary<string, IEnumerable<string>> modsToPlugins,
            IDictionary<string, IEnumerable<string>> pluginsToMods)
        {
            this.modsToPlugins = modsToPlugins;
            this.pluginsToMods = pluginsToMods;
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

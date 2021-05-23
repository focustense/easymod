using System;
using System.IO;
using System.Linq;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditModPluginMapFactory : IModPluginMapFactory
    {
        public ModPluginMap CreateForDirectory(string modRootDirectory)
        {
            var pluginNames = Setup.GetLoadedFileNames();
            var archiveNames = Resources.GetLoadedContainers().Select(f => Path.GetFileName(f));
            return ModPluginMap.ForDirectory(modRootDirectory, pluginNames, archiveNames);
        }
    }
}
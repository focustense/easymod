using System;
using System.Collections.Generic;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerModResolver : IModResolver
    {
        private readonly ModOrganizerConfiguration config;

        public ModOrganizerModResolver(string exePath)
        {
            var directoryPath = Path.GetDirectoryName(exePath);
            var entryIniPath = Path.Combine(directoryPath, "ModOrganizer.ini");
            config = new ModOrganizerConfiguration(entryIniPath);
        }

        public string GetDefaultModRootDirectory()
        {
            return config.ModsDirectory;
        }

        public IEnumerable<string> GetModDirectories(string modName)
        {
            return new[] { modName };
        }

        public string GetModName(string directoryPath)
        {
            return new DirectoryInfo(directoryPath).Name;
        }
    }
}
using System.Collections.Generic;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerModResolver : IModResolver
    {
        private readonly IModOrganizerConfiguration config;

        public ModOrganizerModResolver(IModOrganizerConfiguration config)
        {
            this.config = config;
        }

        public string? GetDefaultModRootDirectory()
        {
            return config?.ModsDirectory;
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
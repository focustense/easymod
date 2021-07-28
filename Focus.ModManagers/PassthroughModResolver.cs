using System.Collections.Generic;
using System.IO;

namespace Focus.ModManagers
{
    public class PassthroughModResolver : IModResolver
    {
        public string? GetDefaultModRootDirectory()
        {
            return null;
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
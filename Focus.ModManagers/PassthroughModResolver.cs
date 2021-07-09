using System;
using System.Collections.Generic;
using System.IO;

namespace Focus.ModManagers
{
    public class PassthroughModResolver : IModResolver
    {
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
using System;
using System.Collections.Generic;

namespace Focus.ModManagers
{
    public interface IModResolver
    {
        string? GetDefaultModRootDirectory();
        IEnumerable<string> GetModDirectories(string modName);
        string GetModName(string directoryPath);
    }
}

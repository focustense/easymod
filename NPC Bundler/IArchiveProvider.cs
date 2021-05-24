using System;
using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IArchiveProvider
    {
        IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "");
        IEnumerable<string> GetLoadedArchivePaths();
    }
}
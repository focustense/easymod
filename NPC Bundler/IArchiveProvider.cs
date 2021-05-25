using System;
using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IArchiveProvider
    {
        void CopyToFile(string archivePath, string archiveFilePath, string outFilePath);
        IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "");
        IEnumerable<string> GetLoadedArchivePaths();
        string ResolvePath(string archiveName);
    }
}
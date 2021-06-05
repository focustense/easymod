using System;
using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IArchiveProvider
    {
        bool ContainsFile(string archivePath, string archiveFilePath);
        void CopyToFile(string archivePath, string archiveFilePath, string outFilePath);
        IGameFileProvider CreateGameFileProvider();
        IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "");
        IEnumerable<string> GetLoadedArchivePaths();
        string ResolvePath(string archiveName);
    }
}
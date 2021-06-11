using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc
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
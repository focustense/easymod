using System;
using System.Collections.Generic;

namespace Focus.Files
{
    public interface IArchiveProvider
    {
        bool ContainsFile(string archivePath, string archiveFilePath);
        IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "");
        string GetArchivePath(string archiveName);
        IEnumerable<string> GetLoadedArchivePaths();
        ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath);
    }
}
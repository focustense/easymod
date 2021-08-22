using System;
using System.Collections.Generic;

namespace Focus.Files
{
    public interface IArchiveProvider
    {
        bool ContainsFile(string archivePath, string archiveFilePath);
        IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "");
        IEnumerable<string> GetBadArchivePaths();
        bool IsArchiveFile(string path);
        ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath);
    }
}

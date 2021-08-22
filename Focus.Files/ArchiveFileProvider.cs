using System;

namespace Focus.Files
{
    public class ArchiveFileProvider : IFileProvider
    {
        private readonly string archivePath;
        private readonly IArchiveProvider archiveProvider;

        public ArchiveFileProvider(IArchiveProvider archiveProvider, string archivePath)
        {
            this.archivePath = archivePath;
            this.archiveProvider = archiveProvider;
        }

        public bool Exists(string fileName)
        {
            return archiveProvider.ContainsFile(archivePath, fileName);
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return archiveProvider.ReadBytes(archivePath, fileName);
        }
    }
}
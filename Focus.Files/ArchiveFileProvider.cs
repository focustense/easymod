using System;
using System.IO;
using System.Threading.Tasks;

namespace Focus.Files
{
    public class ArchiveFileProvider : IFileProvider, IAsyncFileProvider
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

        public Task<bool> ExistsAsync(string fileName)
        {
            return Task.FromResult(Exists(fileName));
        }

        public ulong GetSize(string fileName)
        {
            return archiveProvider.GetArchiveFileSize(archivePath, fileName);
        }

        public Task<ulong> GetSizeAsync(string fileName)
        {
            return Task.FromResult(GetSize(fileName));
        }

        public Task<Stream> GetStreamAsync(string fileName)
        {
            return Task.FromResult(archiveProvider.GetFileStream(archivePath, fileName));
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return archiveProvider.ReadBytes(archivePath, fileName);
        }

        public async Task<Memory<byte>> ReadBytesAsync(string fileName)
        {
            using var stream = await GetStreamAsync(fileName);
            var data = new byte[stream.Length];
            await stream.ReadAsync(data);
            return data;
        }
    }
}
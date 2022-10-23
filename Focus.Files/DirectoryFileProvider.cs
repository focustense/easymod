using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Focus.Files
{
    public class DirectoryFileProvider : IFileProvider, IAsyncFileProvider
    {
        private const int ASYNC_BUFFER_SIZE = 1; // Disable buffering

        private readonly IFileSystem fs;
        private readonly string rootPath;

        public DirectoryFileProvider(IFileSystem fs, string rootPath)
        {
            this.fs = fs;
            this.rootPath = rootPath;
        }

        public bool Exists(string fileName)
        {
            return fs.File.Exists(fs.Path.Combine(rootPath, fileName));
        }

        public Task<bool> ExistsAsync(string fileName)
        {
            return Task.FromResult(Exists(fileName));
        }

        public ulong GetSize(string fileName)
        {
            var fi = fs.FileInfo.FromFileName(ResolvePath(fileName));
            return fi.Exists ? (ulong)fi.Length : 0;
        }

        public Task<ulong> GetSizeAsync(string fileName)
        {
            return Task.FromResult(GetSize(fileName));
        }

        public Task<Stream> GetStreamAsync(string fileName)
        {
            // The stream itself has to be opened synchronously, but calling `GetStreamAsync` is
            // still important to let us know that it should be opened in async mode.
            // Not specifying isAsync: true will make async operations perform poorly.
            return Task.FromResult(
                fs.FileStream.Create(
                    ResolvePath(fileName),
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    ASYNC_BUFFER_SIZE,
                    /* isAsync= */ true));
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return fs.File.ReadAllBytes(ResolvePath(fileName));
        }

        public async Task<Memory<byte>> ReadBytesAsync(string fileName)
        {
            return await fs.File.ReadAllBytesAsync(ResolvePath(fileName));
        }

        private string ResolvePath(string fileName)
        {
            return fs.Path.Combine(rootPath, fileName);
        }
    }
}
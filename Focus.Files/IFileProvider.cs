using System;
using System.IO;
using System.Threading.Tasks;

namespace Focus.Files
{
    public interface IFileProvider
    {
        bool Exists(string fileName);
        ulong GetSize(string fileName);
        ReadOnlySpan<byte> ReadBytes(string fileName);
    }

    public interface IAsyncFileProvider
    {
        Task<bool> ExistsAsync(string fileName);
        Task<ulong> GetSizeAsync(string fileName);
        Task<Stream> GetStreamAsync(string fileName);
        Task<Memory<byte>> ReadBytesAsync(string fileName);
    }
}
using System;

namespace Focus.Files
{
    public interface IFileProvider
    {
        bool Exists(string fileName);
        ulong GetSize(string fileName);
        ReadOnlySpan<byte> ReadBytes(string fileName);
    }
}
using System;
using System.IO;
using System.IO.Abstractions;

namespace Focus.Files
{
    public class DirectoryFileProvider : IFileProvider
    {
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

        public ulong GetSize(string fileName)
        {
            var fi = fs.FileInfo.FromFileName(fs.Path.Combine(rootPath, fileName));
            return fi.Exists ? (ulong)fi.Length : 0;
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return fs.File.ReadAllBytes(Path.Combine(rootPath, fileName));
        }
    }
}
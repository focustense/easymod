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
            return fs.File.Exists(Path.Combine(rootPath, fileName));
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return fs.File.ReadAllBytes(Path.Combine(rootPath, fileName));
        }
    }
}
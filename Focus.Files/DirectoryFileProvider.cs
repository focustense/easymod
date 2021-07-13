using System;
using System.IO;

namespace Focus.Files
{
    public class DirectoryFileProvider : IFileProvider
    {
        private readonly string rootPath;

        public DirectoryFileProvider(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(rootPath, fileName));
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return File.ReadAllBytes(Path.Combine(rootPath, fileName));
        }
    }
}
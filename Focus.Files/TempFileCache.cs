using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;

namespace Focus.Files
{
    public class TempFileCache : IDisposable
    {
        private readonly IFileSystem fs;
        private readonly ConcurrentDictionary<string, string> pathMap = new(StringComparer.OrdinalIgnoreCase);

        private bool disposed;

        public TempFileCache(IFileSystem fs)
        {
            this.fs = fs;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Purge();
                GC.SuppressFinalize(this);
            }
        }

        public string GetTempPath(IFileProvider fileProvider, string sourceFileName)
        {
            return pathMap.GetOrAdd(sourceFileName, _ =>
            {
                var data = fileProvider.ReadBytes(sourceFileName);
                var path = fs.Path.GetTempFileName();
                using var stream = fs.File.Create(path);
                stream.Write(data);
                stream.Flush();
                return path;
            });
        }

        public void Purge()
        {
            try
            {
                foreach (var destPath in pathMap.Values)
                    fs.File.Delete(destPath);
            }
            catch (IOException)
            {
                // Ignore errors here, they're just temp files and there's nothing else we can do.
            }
            pathMap.Clear();
        }
    }
}
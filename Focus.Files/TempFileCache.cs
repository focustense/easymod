using System;
using System.Collections.Concurrent;
using System.IO;

namespace Focus.Files
{
    public class TempFileCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, string> pathMap = new(StringComparer.OrdinalIgnoreCase);

        private bool disposed;

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
                var path = Path.GetTempFileName();
                using var fs = File.Create(path);
                fs.Write(data);
                fs.Flush();
                return path;
            });
        }

        public void Purge()
        {
            try
            {
                foreach (var destPath in pathMap.Values)
                    File.Delete(destPath);
            }
            catch (IOException)
            {
                // Ignore errors here, they're just temp files and there's nothing else we can do.
            }
            pathMap.Clear();
        }
    }
}
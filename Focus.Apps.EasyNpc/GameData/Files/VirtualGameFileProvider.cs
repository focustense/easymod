using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.GameData.Files
{
    public class VirtualGameFileProvider : IGameFileProvider
    {
        private readonly IReadOnlyList<string> archivePriorities;
        private readonly IArchiveProvider archiveProvider;
        private readonly string gameDataDirectory;
        private readonly ConcurrentDictionary<string, PhysicalFileInfo> physicalFileMap =
            new(StringComparer.OrdinalIgnoreCase);

        private bool disposed;

        public VirtualGameFileProvider(string gameDataDirectory, IArchiveProvider archiveProvider)
        {
            this.gameDataDirectory = gameDataDirectory;
            this.archiveProvider = archiveProvider;
            // This lookup can be expensive, so only do it once.
            archivePriorities = archiveProvider.GetLoadedArchivePaths().ToList().AsReadOnly();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                PurgeTempFiles();
                physicalFileMap.Clear();
                GC.SuppressFinalize(this);
            }
        }

        public bool Exists(string fileName)
        {
            return !GetPhysicalFileInfo(fileName).IsMissing();
        }

        public string GetPhysicalPath(string fileName)
        {
            var info = GetPhysicalFileInfo(fileName);
            if (info.IsMissing())
                throw new Exception($"File {fileName} does not exist in current game data or archives.");
            if (!string.IsNullOrEmpty(info.LooseFilePath))
                return info.LooseFilePath;

            // Why this dumb hack of extracting from archives to a physical path, and then having callers read the new
            // file? Mainly for the anemic SWIG methods we get in NiflySharp, which support .NET file paths (i.e.
            // strings) alright, but don't have any support for std::istream. Mutagen and other BSA libraries can open
            // up a stream just fine, directly from the archive, but that doesn't do us any good here.
            //
            // The lock is to avoid generating duplicate temp files for the same input file, in case the same file is
            // requested by multiple threads at the same time. Everything else in here is already thread-safe via the
            // use of concurrent collections.
            lock (info)
            {
                if (!string.IsNullOrEmpty(info.TempFilePath))
                    return info.TempFilePath;
                var tempFilePath = Path.GetTempFileName();
                archiveProvider.CopyToFile(info.ContainingArchivePath, fileName, tempFilePath);
                info.TempFilePath = tempFilePath;
                return tempFilePath;
            }
        }

        private PhysicalFileInfo GetPhysicalFileInfo(string virtualFileName)
        {
            if (physicalFileMap.TryGetValue(virtualFileName, out var cachedInfo))
                return cachedInfo;
            var looseFilePath = Path.Combine(gameDataDirectory, virtualFileName);
            if (File.Exists(looseFilePath))
            {
                var info = new PhysicalFileInfo { LooseFilePath = looseFilePath };
                physicalFileMap.TryAdd(virtualFileName, info);
                return info;
            }
            var bestArchive = archivePriorities
                .Where(f => archiveProvider.ContainsFile(f, virtualFileName))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(bestArchive))
            {
                var info = new PhysicalFileInfo { ContainingArchivePath = bestArchive };
                physicalFileMap.TryAdd(virtualFileName, info);
                return info;
            }
            else
            {
                physicalFileMap.TryAdd(virtualFileName, PhysicalFileInfo.None);
                return PhysicalFileInfo.None;
            }
        }

        private void PurgeTempFiles()
        {
            foreach (var info in physicalFileMap.Values)
                if (!string.IsNullOrEmpty(info.TempFilePath))
                    File.Delete(info.TempFilePath);
        }

        class PhysicalFileInfo
        {
            public static readonly PhysicalFileInfo None = new PhysicalFileInfo();

            public string ContainingArchivePath { get; init; }
            public string LooseFilePath { get; init; }
            public string TempFilePath { get; set; }

            public bool IsMissing()
            {
                return string.IsNullOrEmpty(LooseFilePath) && string.IsNullOrEmpty(ContainingArchivePath);
            }
        }
    }
}
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.GameData.Files
{
    // Maintains a global bidirectional map of archives (i.e. BSAs) to facegen files in those archives.
    //
    // Enumerating a BSA doesn't actually seem to be a very expensive operation, at least on a modern machine; but this
    // time could still add up on lower-end machines, especially if there are hundreds of mods and some of them provide
    // huge numbers of facegen files (Skyrim - Meshes0, obviously, but also 3DNPC and other large overhauls), and if we
    // have to call this repeatedly for any reason.
    public class ArchiveFileMap
    {
        private Dictionary<string, IEnumerable<string>> archivesToFiles;
        private Dictionary<string, IEnumerable<string>> filesToArchives;
        private readonly ILogger log;

        private readonly IArchiveProvider archiveProvider;

        public ArchiveFileMap(IArchiveProvider archiveProvider, ILogger log)
        {
            this.archiveProvider = archiveProvider;
            this.log = log;
        }

        // Most code shouldn't need to call this, unless the map is going to be subsequently be used by parallel or
        // multithreaded code that might otherwise trigger multiple concurrent initializations.
        //
        // It's safe to call regardless, just generally unnecessary.
        public void EnsureInitialized()
        {
            Initialize();
        }

        public bool ContainsFile(string archiveName, string fileName)
        {
            Initialize();
            return GetFilesInArchive(archiveName).Any(f => f.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetArchivesContainingFile(string archivedFileName)
        {
            Initialize();
            return filesToArchives.TryGetValue(archivedFileName, out IEnumerable<string> archives) ?
                archives : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFilesInArchive(string archiveName)
        {
            Initialize();
            return archivesToFiles.TryGetValue(archiveName, out IEnumerable<string> fileNames) ?
                fileNames : Enumerable.Empty<string>();
        }

        private void Initialize()
        {
            if (archivesToFiles != null || filesToArchives != null)
                return;
            log.Information("Building file index from loaded archives");
            var archivesWithFiles = archiveProvider.GetLoadedArchivePaths()
                .Tap(path => log.Debug("Indexing archive {archivePath}", path))
                .Select(path => new
                {
                    FileName = Path.GetFileName(path),
                    FaceGenFiles = archiveProvider.GetArchiveFileNames(path, FileStructure.FaceMeshesPath)
                        .Concat(archiveProvider.GetArchiveFileNames(path, FileStructure.FaceTintsPath)),
                })
                .Tap(x => log.Information("Indexed archive {archivePath}", x.FileName))
                .ToList();
            Parallel.Invoke(
                () =>
                {
                    archivesToFiles = archivesWithFiles.ToDictionary(
                        x => x.FileName,
                        x => x.FaceGenFiles.ToList().AsReadOnly().AsEnumerable());
                },
                () =>
                {
                    filesToArchives = archivesWithFiles
                        .SelectMany(x => x.FaceGenFiles.Select(f => new { ArchiveFile = x.FileName, FaceGenFile = f }))
                        .GroupBy(x => x.FaceGenFile, x => x.ArchiveFile)
                        .ToDictionary(x => x.Key, x => x.ToList().AsReadOnly().AsEnumerable());
                });
        }
    }
}

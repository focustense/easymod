using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Storage.Archives
{
    public class ArchiveBuilder
    {
        private readonly List<Action<FileEntry>> addedActions = new();
        private readonly List<Action<FileEntry>> addingActions = new();
        private readonly List<Action<IReadOnlyList<FileEntry>>> beforeBuildActions = new();
        private readonly List<FileEntry> fileEntries = new();
        private readonly ArchiveType type;

        // 2 GB is the maximimum size for the archive itself. Even though some BSAs that are slightly larger than 2 GB
        // may appear to work - i.e. not crash at the loading screen - they are still liable to cause unpredictable
        // crashes or other bugs in the game, depending on which files actually end up in the above-2GB range.
        //
        // It's possible for a BSA to compress extremely well, especially texture archives, such as over 20 GB down to
        // under 2 GB, but as of now, there is no way to determine this in advance. Therefore, the default uncompressed
        // size is still set to 2 GB, which may compress to a much smaller size, and callers can substitute their own
        // larger value instead if they're willing to take some risks.
        private long maxUncompressedSize = 2L * 1024 * 1024 * 1024;
        private bool shouldCompress = false;
        private bool shouldShareData = false;

        public ArchiveBuilder(ArchiveType type)
        {
            this.type = type;
        }

        public ArchiveBuilder AddDirectory(string directoryName, string pathInArchive, string searchPattern = null)
        {
            var entries = Directory.GetFiles(directoryName, searchPattern ?? "*.*", SearchOption.AllDirectories)
                .Select(f => new FileEntry
                {
                    LocalFilePath = f,
                    PathInArchive = Path.Combine(pathInArchive, Path.GetRelativePath(directoryName, f)),
                    Size = GetFileSize(f),
                });
            fileEntries.AddRange(entries);
            return this;
        }

        public ArchiveBuilder AddFile(string fileName, string pathInArchive)
        {
            fileEntries.Add(new FileEntry {
                LocalFilePath = fileName,
                PathInArchive = pathInArchive,
                Size = GetFileSize(fileName)
            });
            return this;
        }

        public BuildResult Build(string outputFileName)
        {
            foreach (var beforeBuildAction in beforeBuildActions)
                beforeBuildAction(fileEntries.AsReadOnly());
            var fileGroups = SplitEntries(outputFileName);
            var archiveResults = new List<ArchiveResult>();
            Parallel.ForEach(fileGroups, group =>
            {
                var archive = Functions.BsaCreate();
                Functions.BsaCompressSet(archive, shouldCompress);
                Functions.BsaShareDataSet(archive, shouldShareData);
                using var manifest = new ArchiveManifest().AddAll(group.Entries.Select(x => x.PathInArchive));
                Functions.BsaCreateArchive(archive, group.FileName, type, manifest.Handle);
                // We probably don't get amazing throughput from this, because the writes are synchronized in libbsarch
                // (which is also why this is thread-safe). However, we do get some, because the files are compressed, which
                // is not trivial on CPU and can happen at the same time as writes.
                Parallel.ForEach(group.Entries, fileEntry =>
                {
                    foreach (var addingAction in addingActions)
                        addingAction(fileEntry);
                    Functions.BsaAddFileFromDisk(archive, fileEntry.PathInArchive, fileEntry.LocalFilePath);
                    foreach (var addedAction in addedActions)
                        addedAction(fileEntry);
                });
                var saveResult = Functions.BsaSave(archive);
                Debug.WriteLine($"Save result: {saveResult.Code} - {saveResult.Text}");
                archiveResults.Add(new ArchiveResult(group.FileName, archive));
            });
            return new BuildResult(archiveResults);
        }

        public ArchiveBuilder Compress(bool compress)
        {
            shouldCompress = compress;
            return this;
        }

        public ArchiveBuilder MaxUncompressedSize(long maxUncompressedSize)
        {
            this.maxUncompressedSize = maxUncompressedSize;
            return this;
        }
        
        public ArchiveBuilder OnBeforeBuild(Action<IReadOnlyList<FileEntry>> action)
        {
            beforeBuildActions.Add(action);
            return this;
        }

        public ArchiveBuilder OnPacked(Action<FileEntry> action)
        {
            addedActions.Add(action);
            return this;
        }

        public ArchiveBuilder OnPacking(Action<FileEntry> action)
        {
            addingActions.Add(action);
            return this;
        }

        public ArchiveBuilder ShareData(bool shareData)
        {
            shouldShareData = shareData;
            return this;
        }

        private static long GetFileSize(string fileName)
        {
            return new FileInfo(fileName).Length;
        }

        private static string GetOverflowFileName(string outputFileName, int overflowIndex)
        {
            var extension = Path.GetExtension(outputFileName);
            var directoryName = Path.GetDirectoryName(outputFileName);
            var fileName = Path.GetFileNameWithoutExtension(outputFileName);
            var pathWithoutExtension = Path.Combine(directoryName, fileName);
            return $"{pathWithoutExtension}{overflowIndex}{extension}";
        }

        private IEnumerable<FileGroup> SplitEntries(string outputFileName)
        {
            var group = new FileGroup { FileName = outputFileName };
            var nextOverflowIndex = 0;
            foreach (var entry in fileEntries)
            {
                if (group.Count > 0 && group.Size + entry.Size > maxUncompressedSize)
                {
                    yield return group;
                    group = new FileGroup { FileName = GetOverflowFileName(outputFileName, nextOverflowIndex++) };
                }
                group.Add(entry);
            }
            yield return group;
        }

        public class ArchiveResult
        {
            public Archive Archive { get; private init; }
            public string FileName { get; private init; }

            public ArchiveResult(string fileName, IntPtr handle)
            {
                FileName = fileName;
                Archive = new Archive(handle);
            }
        }

        public class BuildResult
        {
            public IReadOnlyList<ArchiveResult> ArchiveResults { get; init; }

            public BuildResult(IEnumerable<ArchiveResult> archiveResults)
            {
                ArchiveResults = archiveResults.ToList().AsReadOnly();
            }
        }

        public class FileEntry
        {
            public string LocalFilePath { get; init; }
            public string PathInArchive { get; init; }
            public long Size { get; init; }
        }

        class FileGroup
        {
            public int Count => entries.Count;
            public IEnumerable<FileEntry> Entries => entries;
            public string FileName { get; init; }
            public long Size { get; private set; }

            private readonly List<FileEntry> entries = new();

            public void Add(FileEntry entry)
            {
                entries.Add(entry);
                Size += entry.Size;
            }
        }
    }
}
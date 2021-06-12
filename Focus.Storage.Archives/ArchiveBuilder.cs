using System;
using System.Collections.Generic;
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
                    PathInArchive = Path.Combine(pathInArchive, Path.GetRelativePath(directoryName, f))
                });
            fileEntries.AddRange(entries);
            return this;
        }

        public ArchiveBuilder AddFile(string fileName, string pathInArchive)
        {
            fileEntries.Add(new FileEntry { LocalFilePath = fileName, PathInArchive = pathInArchive });
            return this;
        }

        public Archive Build(string outputFileName)
        {
            foreach (var beforeBuildAction in beforeBuildActions)
                beforeBuildAction(fileEntries.AsReadOnly());
            var archive = Functions.BsaCreate();
            Functions.BsaCompressSet(archive, shouldCompress);
            Functions.BsaShareDataSet(archive, shouldShareData);
            var manifest = new ArchiveManifest().AddAll(fileEntries.Select(x => x.PathInArchive));
            Functions.BsaCreateArchive(archive, outputFileName, type, manifest.Handle);
            // We probably don't get amazing throughput from this, because the writes are synchronized in libbsarch
            // (which is also why this is thread-safe). However, we do get some, because the files are compressed, which
            // is not trivial on CPU and can happen at the same time as writes.
            Parallel.ForEach(fileEntries, fileEntry =>
            {
                foreach (var addingAction in addingActions)
                    addingAction(fileEntry);
                Functions.BsaAddFileFromDisk(archive, fileEntry.PathInArchive, fileEntry.LocalFilePath);
                foreach (var addedAction in addedActions)
                    addedAction(fileEntry);
            });
            Functions.BsaSave(archive);
            return new Archive(archive);
        }

        public ArchiveBuilder Compress(bool compress)
        {
            shouldCompress = compress;
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

        public class FileEntry
        {
            public string LocalFilePath { get; init; }
            public string PathInArchive { get; init; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Storage.Archives
{
    public class ArchiveBuilder
    {
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
            var archive = Functions.BsaCreate();
            Functions.BsaCompressSet(archive, shouldCompress);
            Functions.BsaShareDataSet(archive, shouldShareData);
            var manifest = new ArchiveManifest().AddAll(fileEntries.Select(x => x.PathInArchive));
            Functions.BsaCreateArchive(archive, outputFileName, type, manifest.Handle);
            foreach (var fileEntry in fileEntries)
                Functions.BsaAddFileFromDisk(archive, fileEntry.PathInArchive, fileEntry.LocalFilePath);
            Functions.BsaSave(archive);
            return new Archive(archive);
        }

        public ArchiveBuilder Compress(bool compress)
        {
            shouldCompress = compress;
            return this;
        }

        public ArchiveBuilder ShareData(bool shareData)
        {
            shouldShareData = shareData;
            return this;
        }

        class FileEntry
        {
            public string LocalFilePath { get; init; }
            public string PathInArchive { get; init; }
        }
    }
}
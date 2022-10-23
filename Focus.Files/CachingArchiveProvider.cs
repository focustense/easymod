using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Files
{
    public class CachingArchiveProvider : IArchiveProvider
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> fileNameCache = new(PathComparer.Default);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, uint>> fileSizeCache =
            new(PathComparer.Default);
        private readonly IFileSystem fs;
        private readonly IArchiveProvider innerProvider;

        public CachingArchiveProvider(IArchiveProvider innerProvider) :
            this(new FileSystem(), innerProvider)
        { }

        public CachingArchiveProvider(IFileSystem fs, IArchiveProvider innerProvider)
        {
            this.fs = fs;
            this.innerProvider = innerProvider;
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            var allFiles = GetArchiveFileNamesInternal(archivePath);
            return allFiles.Contains(archiveFilePath);
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "")
        {
            var allFiles = GetArchiveFileNamesInternal(archivePath);
            var prefix = PathComparer.NormalizePath(path);
            if (!string.IsNullOrEmpty(prefix))
                prefix += fs.Path.DirectorySeparatorChar;
            return allFiles.Where(p =>
                string.IsNullOrEmpty(prefix) || PathComparer.NormalizePath(p).StartsWith(prefix));
        }

        public uint GetArchiveFileSize(string archivePath, string archiveFilePath)
        {
            var sizes = fileSizeCache.GetOrAdd(archivePath, _ => new(PathComparer.Default));
            return sizes.GetOrAdd(
                archiveFilePath,
                _ => ContainsFile(archivePath, archiveFilePath) ?
                    innerProvider.GetArchiveFileSize(archivePath, archiveFilePath) : 0);
        }

        public IEnumerable<string> GetBadArchivePaths()
        {
            // This list could change at any time, so we don't want to cache it. Providers shouldn't scan every archive
            // every time we request this - instead it's passively updated whenever a bad archive is detected.
            return innerProvider.GetBadArchivePaths();
        }

        public Stream GetFileStream(string archivePath, string archiveFilePath)
        {
            return innerProvider.GetFileStream(archivePath, archiveFilePath);
        }

        public bool IsArchiveFile(string path)
        {
            return innerProvider.IsArchiveFile(path);
        }

        public ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath)
        {
            return innerProvider.ReadBytes(archivePath, archiveFilePath);
        }

        private HashSet<string> GetArchiveFileNamesInternal(string archivePath)
        {
            return fileNameCache.GetOrAdd(archivePath, _ =>
                innerProvider.GetArchiveFileNames(archivePath).ToHashSet(PathComparer.Default));
        }
    }
}

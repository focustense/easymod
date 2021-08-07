using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Files.Tests
{
    // Mocks don't work well with IArchiveProvider due to the use of ReadOnlySpan.
    class FakeArchiveProvider : IArchiveProvider
    {
        private readonly Dictionary<string, Dictionary<string, byte[]>> files = new();

        public string[] ArchiveExtensions { get; set; } = new[] { ".bsa" };
        public string[] BadArchivePaths { get; set; } = Array.Empty<string>();
        public string[] LoadedArchivePaths { get; set; } = Array.Empty<string>();

        public void AddFile(string archivePath, string archiveFilePath, byte[] data)
        {
            GetArchiveFiles(archivePath).Add(archiveFilePath, data);
        }

        public void AddFiles(string archivePath, params string[] archiveFilePaths)
        {
            foreach (var filePath in archiveFilePaths)
                AddFile(archivePath, filePath, Array.Empty<byte>());
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            return GetArchiveFiles(archivePath).ContainsKey(archiveFilePath);
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "")
        {
            return GetArchiveFiles(archivePath).Keys
                .Where(f => string.IsNullOrEmpty(path) || f.StartsWith(path));
        }

        public string GetArchivePath(string archiveName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetBadArchivePaths()
        {
            return BadArchivePaths;
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            return LoadedArchivePaths;
        }

        public bool IsArchiveFile(string path)
        {
            return ArchiveExtensions.Contains(Path.GetExtension(path));
        }

        public ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath)
        {
            var archiveFiles = GetArchiveFiles(archivePath);
            return archiveFiles.TryGetValue(archiveFilePath, out var data) ? data : null;
        }

        private Dictionary<string, byte[]> GetArchiveFiles(string archivePath)
        {
            if (!files.TryGetValue(archivePath, out var archiveFiles))
            {
                archiveFiles = new();
                files.Add(archivePath, archiveFiles);
            }
            return archiveFiles;
        }
    }
}
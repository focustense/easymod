using Focus.Files;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Providers.Mutagen
{
    public class MutagenArchiveProvider : IArchiveProvider
    {
        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;

        public MutagenArchiveProvider(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment)
        {
            this.environment = environment;
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            return reader.Files.Any(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "")
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            return reader.Files
                .Where(f => string.IsNullOrEmpty(path) || f.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path);
        }

        public string GetArchivePath(string archiveName)
        {
            return Path.Combine(environment.DataFolderPath, archiveName);
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            var dataFolderPath = new DirectoryPath(environment.DataFolderPath);
            return Archive.GetApplicableArchivePaths(GameRelease.SkyrimSE, dataFolderPath).Select(x => x.Path);
        }

        public ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            var folderName = Path.GetDirectoryName(archiveFilePath).ToLower();  // Mutagen is case-sensitive
            if (!reader.TryGetFolder(folderName, out var folder))
                throw new Exception($"Couldn't find folder {folderName} in archive {archivePath}");
            var file = folder.Files
                .SingleOrDefault(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            if (file == null)
                throw new Exception($"Couldn't find file {archiveFilePath} in archive {archivePath}");
            return file.GetSpan();
        }
    }
}
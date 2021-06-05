using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NPC_Bundler
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

        public void CopyToFile(string archivePath, string archiveFilePath, string outFilePath)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            var folderName = Path.GetDirectoryName(archiveFilePath).ToLower();  // Mutagen is case-sensitive
            if (!reader.TryGetFolder(folderName, out var folder))
                throw new Exception($"Couldn't find folder {folderName} in archive {archivePath}");
            var file = folder.Files
                .SingleOrDefault(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            if (file == null)
                throw new Exception($"Couldn't find file {archiveFilePath} in archive {archivePath}");
            using var fs = File.Create(outFilePath);
            file.CopyDataTo(fs);
            fs.Flush(); // Is it necessary?
        }

        public IGameFileProvider CreateGameFileProvider()
        {
            return new VirtualGameFileProvider(environment.GameFolderPath, this);
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            return reader.Files
                .Where(f => string.IsNullOrEmpty(path) || f.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path);
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            // Currently, GetApplicableArchivePaths ignores the ModKey entirely and just reads every available BSA.
            // This is actually fine for our purposes, but the code has to be aware of this to avoid duplication.
            return Archive.GetApplicableArchivePaths(GameRelease.SkyrimSE, environment.GameFolderPath, ModKey.Null);
        }

        public string ResolvePath(string archiveName)
        {
            return Path.Combine(environment.GameFolderPath, archiveName);
        }
    }
}
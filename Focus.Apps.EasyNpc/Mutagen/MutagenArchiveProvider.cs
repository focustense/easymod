#nullable enable

using Focus.Apps.EasyNpc.GameData.Files;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Archives.Exceptions;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenArchiveProvider : IArchiveProvider
    {
        private readonly HashSet<string> badArchivePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;
        private readonly ILogger log;

        public MutagenArchiveProvider(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, ILogger log)
        {
            this.environment = environment;
            this.log = log;
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            return Safe(archivePath, () =>
            {
                var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
                return reader.Files.Any(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            });
        }

        public void CopyToFile(string archivePath, string archiveFilePath, string outFilePath)
        {
            Safe(archivePath, () =>
            {
                var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
                var folderName = Path.GetDirectoryName(archiveFilePath)!.ToLower();  // Mutagen is case-sensitive
                if (!reader.TryGetFolder(folderName, out var folder))
                    throw new Exception($"Couldn't find folder {folderName} in archive {archivePath}");
                var file = folder.Files
                    .SingleOrDefault(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
                if (file == null)
                    throw new Exception($"Couldn't find file {archiveFilePath} in archive {archivePath}");
                using var fs = File.Create(outFilePath);
                fs.Write(file.GetSpan());
                fs.Flush(); // Is it necessary?
                return true; // Dummy value for Safe()
            });
        }

        public IGameFileProvider CreateGameFileProvider()
        {
            return new VirtualGameFileProvider(environment.DataFolderPath, this);
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path)
        {
            return Safe(archivePath, () =>
            {
                var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
                return reader.Files
                    .Where(f =>
                        string.IsNullOrEmpty(path) || f.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Path);
            }) ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            var dataFolderPath = new DirectoryPath(environment.DataFolderPath);
            return Archive.GetApplicableArchivePaths(GameRelease.SkyrimSE, dataFolderPath).Select(x => x.Path);
        }

        public string ResolvePath(string archiveName)
        {
            return Path.Combine(environment.DataFolderPath, archiveName);
        }

        private T? Safe<T>(string archivePath, Func<T> action)
        {
            if (badArchivePaths.Contains(archivePath))
                return default;
            try
            {
                return action();
            }
            catch (InvalidDataException ex)
            {
                // This type of error happens in the BsaFileNameBlock and means we'll never be able to use the archive.
                log.Error(ex, "Archive {archivePath} is invalid, corrupt or unreadable", archivePath);
                badArchivePaths.Add(archivePath);
            }
            catch (ArchiveException ex)
            {
                // This could happen with an individual file; other parts of the archive may still be readable.
                log.Error(ex, "Problem reading from archive {archivePath}", archivePath);
            }
            return default;
        }
    }
}
using Focus.Files;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchiveException = Focus.Files.ArchiveException;
using MutagenArchiveException = Mutagen.Bethesda.Archives.Exceptions.ArchiveException;

namespace Focus.Providers.Mutagen
{
    public class MutagenArchiveProvider : IArchiveProvider
    {
        private readonly IArchiveStatics archive;
        private readonly HashSet<string> badArchivePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> environment;
        private readonly GameRelease gameRelease;
        private readonly ILogger log;
        private readonly IReadOnlyList<FileName> order;

        public MutagenArchiveProvider(
            GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, GameRelease gameRelease, ILogger log)
            : this(GameEnvironmentWrapper.Wrap(environment), ArchiveStaticsWrapper.Instance, gameRelease, log)
        {
        }

        public MutagenArchiveProvider(
            IReadOnlyGameEnvironment<ISkyrimModGetter> environment, GameRelease gameRelease, ILogger log)
            : this(environment, ArchiveStaticsWrapper.Instance, gameRelease, log)
        {
        }

        public MutagenArchiveProvider(
            IReadOnlyGameEnvironment<ISkyrimModGetter> environment, IArchiveStatics archive, GameRelease gameRelease, ILogger log)
        {
            this.environment = environment;
            this.archive = archive;
            this.gameRelease = gameRelease;
            this.log = log;
            order = archive.GetIniListings(gameRelease)
                .Concat(environment.LoadOrder.ListedOrder.SelectMany(x => new[] {
                    // Not all of these will exist, but it doesn't matter, as these are only used for sorting and won't
                    // affect the actual set of paths returned.
                    new FileName($"{x.ModKey.Name}.bsa"),
                    new FileName($"{x.ModKey.Name} - Textures.bsa"),
                }))
                .ToList();
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            return Safe(archivePath, () =>
            {
                var reader = archive.CreateReader(gameRelease, archivePath);
                return reader.Files.Any(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            });
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "")
        {
            return Safe(archivePath, () =>
            {
                var prefix = NormalizePath(path);
                var reader = archive.CreateReader(gameRelease, archivePath);
                return reader.Files
                    .Select(f => Safe(archivePath, () => f.Path))
                    .NotNull()
                    .Where(p =>
                        string.IsNullOrEmpty(prefix) ||
                        NormalizePath(p).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }) ?? Enumerable.Empty<string>();
        }

        public string GetArchivePath(string archiveName)
        {
            return Path.Combine(environment.GetRealDataDirectory(), archiveName);
        }

        public IEnumerable<string> GetBadArchivePaths()
        {
            return badArchivePaths;
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            var dataFolderPath = new DirectoryPath(environment.GetRealDataDirectory());
            return archive.GetApplicableArchivePaths(gameRelease, dataFolderPath, order).Select(x => x.Path);
        }

        public ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath)
        {
            var reader = archive.CreateReader(gameRelease, archivePath);
            var folderName = Path.GetDirectoryName(archiveFilePath)?.ToLower();  // Mutagen is case-sensitive
            if (string.IsNullOrEmpty(folderName))
                throw new ArgumentException($"Archive path '{archivePath}' is missing directory info.", nameof(archivePath));
            if (!reader.TryGetFolder(folderName, out var folder))
                throw new ArchiveException($"Couldn't find folder {folderName} in archive {archivePath}");
            var file = folder.Files
                .SingleOrDefault(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            if (file == null)
                throw new ArchiveException($"Couldn't find file {archiveFilePath} in archive {archivePath}");
            return file.GetSpan();
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            if (!Path.EndsInDirectorySeparator(path))
                path += Path.DirectorySeparatorChar;
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
            catch (MutagenArchiveException ex)
            {
                // This could happen with an individual file; other parts of the archive may still be readable.
                log.Error(ex, "Problem reading from archive {archivePath}", archivePath);
            }
            return default;
        }
    }
}

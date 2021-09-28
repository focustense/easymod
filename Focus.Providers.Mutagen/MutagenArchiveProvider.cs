using Focus.Files;
using Mutagen.Bethesda.Archives;
using Noggog;
using Serilog;
using System;
using System.Collections.Concurrent;
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
        private readonly GameSelection game;
        private readonly ILogger log;
        private readonly ConcurrentDictionary<string, IArchiveReader> readers = new(PathComparer.Default);

        public MutagenArchiveProvider(GameSelection game, ILogger log)
            : this(ArchiveStatics.Instance, game, log)
        {
        }

        public MutagenArchiveProvider(IArchiveStatics archive, GameSelection game, ILogger log)
        {
            this.archive = archive;
            this.game = game;
            this.log = log;
        }

        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            return Safe(archivePath, () =>
            {
                var reader = GetReader(archivePath);
                return reader.Files.Any(f => string.Equals(f.Path, archiveFilePath, StringComparison.OrdinalIgnoreCase));
            });
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path = "")
        {
            return Safe(archivePath, () =>
            {
                var prefix = PathComparer.NormalizePath(path);
                if (!string.IsNullOrEmpty(prefix))
                    prefix += Path.DirectorySeparatorChar;
                var reader = GetReader(archivePath);
                return reader.Files
                    .Select(f => Safe(archivePath, () => f.Path))
                    .NotNull()
                    .Where(p =>
                        string.IsNullOrEmpty(prefix) ||
                        PathComparer.NormalizePath(p).StartsWith(prefix));
            }) ?? Enumerable.Empty<string>();
        }

        public uint GetArchiveFileSize(string archivePath, string archiveFilePath)
        {
            var reader = GetReader(archivePath);
            return reader.Files
                .Select(f => Safe(archivePath, () => new { f.Path, Size = new Lazy<uint>(() => f.Size) }))
                .NotNull()
                .Where(x => PathComparer.Default.Equals(x.Path, archiveFilePath))
                .Select(x => x.Size.Value)
                .FirstOrDefault();
        }

        public IEnumerable<string> GetBadArchivePaths()
        {
            return badArchivePaths;
        }

        public bool IsArchiveFile(string path)
        {
            return string.Equals(
                Path.GetExtension(path), archive.GetExtension(game.GameRelease), StringComparison.OrdinalIgnoreCase);
        }

        public ReadOnlySpan<byte> ReadBytes(string archivePath, string archiveFilePath)
        {
            var reader = GetReader(archivePath);
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

        private IArchiveReader GetReader(string archivePath)
        {
            return readers.GetOrAdd(archivePath, _ => archive.CreateReader(game.GameRelease, archivePath));
        }

        private T? Safe<T>(string archivePath, Func<T> action)
        {
            if (badArchivePaths.Contains(archivePath))
                return default;
            try
            {
                return action();
            }
            catch (ArgumentException ex)
            {
                // Can happen with e.g. a bad file name inside the archive.
                log.Error(ex, "Archive {archivePath} is invalid, corrupt or unreadable", archivePath);
                badArchivePaths.Add(archivePath);
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

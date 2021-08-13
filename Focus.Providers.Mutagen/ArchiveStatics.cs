using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Noggog;
using System.Collections.Generic;

namespace Focus.Providers.Mutagen
{
    public interface IArchiveStatics
    {
        IArchiveReader CreateReader(GameRelease gameRelease, FilePath path);
        IEnumerable<FilePath> GetApplicableArchivePaths(
            GameRelease release, DirectoryPath dataFolderPath, IEnumerable<FileName>? archiveOrdering);
        public string GetExtension(GameRelease release);
        IEnumerable<FileName> GetIniListings(GameRelease release);
    }

    public class ArchiveStatics : IArchiveStatics
    {
        public static readonly ArchiveStatics Instance = new();

        public IArchiveReader CreateReader(GameRelease gameRelease, FilePath path)
        {
            return Archive.CreateReader(gameRelease, path);
        }

        public IEnumerable<FilePath> GetApplicableArchivePaths(
            GameRelease release, DirectoryPath dataFolderPath, IEnumerable<FileName>? archiveOrdering)
        {
            return Archive.GetApplicableArchivePaths(release, dataFolderPath, archiveOrdering);
        }

        public string GetExtension(GameRelease release)
        {
            return Archive.GetExtension(release);
        }

        public IEnumerable<FileName> GetIniListings(GameRelease release)
        {
            return Archive.GetIniListings(release);
        }
    }
}

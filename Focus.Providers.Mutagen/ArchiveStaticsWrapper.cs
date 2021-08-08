using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Noggog;
using System.Collections.Generic;

namespace Focus.Providers.Mutagen
{
    public class ArchiveStaticsWrapper : IArchiveStatics
    {
        public static readonly ArchiveStaticsWrapper Instance = new();

        private ArchiveStaticsWrapper() { }

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

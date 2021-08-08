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
}
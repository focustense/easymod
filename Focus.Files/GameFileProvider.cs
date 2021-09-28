using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Files
{
    public class GameFileProvider : CascadingFileProvider
    {
        public GameFileProvider(IGameSettings settings, IArchiveProvider archiveProvider)
            : this(new FileSystem(), settings, archiveProvider) { }

        public GameFileProvider(IFileSystem fs, IGameSettings settings, IArchiveProvider archiveProvider) :
            base(GetFileProviders(fs, settings, archiveProvider)) { }

        private static IEnumerable<IFileProvider> GetFileProviders(
            IFileSystem fs, IGameSettings settings, IArchiveProvider archiveProvider)
        {
            // TODO: This can, and probably should be optimized. The game may have 100 archives; if we need to look up
            // 10,000 files, this becomes surprisingly slow and expensive, especially if a lot of them are misses or
            // only in the base game BSAs (last in the list).
            // We should be able to use ArchiveIndex here as a better alternative. That way we'll know in O(1) time what
            // the highest-priority source for a file is. In theory it might be beneficial to also use a FileIndex for
            // loose files, although that is only one lookup compared to many archives, so maybe not as significant.
            return settings.ArchiveOrder
                .Select(f =>
                    new ArchiveFileProvider(archiveProvider, fs.Path.Combine(settings.DataDirectory, f))
                    as IFileProvider)
                .Prepend(new DirectoryFileProvider(fs, settings.DataDirectory));
        }
    }
}
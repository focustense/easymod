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
            return settings.ArchiveOrder
                .Reverse()  // Listed -> Priority order
                .Select(f =>
                    new ArchiveFileProvider(archiveProvider, fs.Path.Combine(settings.DataDirectory, f))
                    as IFileProvider)
                .Prepend(new DirectoryFileProvider(fs, settings.DataDirectory));
        }
    }
}
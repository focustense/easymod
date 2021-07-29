using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Files
{
    public class GameFileProvider : CascadingFileProvider
    {
        public GameFileProvider(IFileSystem fs, string dataDirectory, IArchiveProvider archiveProvider) :
            base(GetFileProviders(fs, dataDirectory, archiveProvider)) { }

        private static IEnumerable<IFileProvider> GetFileProviders(
            IFileSystem fs, string dataDirectory, IArchiveProvider archiveProvider)
        {
            return archiveProvider
                .GetLoadedArchivePaths()
                .Select(path => new ArchiveFileProvider(archiveProvider, path) as IFileProvider)
                .Prepend(new DirectoryFileProvider(fs, dataDirectory));
        }
    }
}
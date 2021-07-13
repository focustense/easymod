using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class GameFileProvider : CascadingFileProvider
    {
        public GameFileProvider(string dataDirectory, IArchiveProvider archiveProvider) :
            base(GetFileProviders(dataDirectory, archiveProvider)) { }

        private static IEnumerable<IFileProvider> GetFileProviders(
            string dataDirectory, IArchiveProvider archiveProvider)
        {
            return archiveProvider
                .GetLoadedArchivePaths()
                .Select(path => new ArchiveFileProvider(archiveProvider, path) as IFileProvider)
                .Prepend(new DirectoryFileProvider(dataDirectory));
        }
    }
}
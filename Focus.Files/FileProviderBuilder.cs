using System.Collections.Generic;
using System.IO.Abstractions;

namespace Focus.Files
{
    public class FileProviderBuilder
    {
        private readonly IFileSystem fs;
        private readonly IArchiveProvider archiveProvider;
        private readonly List<IFileProvider> fileProviders = new();

        public FileProviderBuilder(IArchiveProvider archiveProvider)
            : this(new FileSystem(), archiveProvider)
        {
        }

        public FileProviderBuilder(IFileSystem fs, IArchiveProvider archiveProvider)
        {
            this.fs = fs;
            this.archiveProvider = archiveProvider;
        }

        public FileProviderBuilder AddArchive(string archivePath)
        {
            fileProviders.Add(new ArchiveFileProvider(archiveProvider, archivePath));
            return this;
        }

        public FileProviderBuilder AddDirectory(string directoryPath)
        {
            fileProviders.Add(new DirectoryFileProvider(fs, directoryPath));
            return this;
        }

        public IAsyncFileProvider Build()
        {
            return new CascadingFileProvider(fileProviders);
        }
    }
}

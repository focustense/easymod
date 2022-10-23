using Focus.Testing.Files;
using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Focus.Files.Tests
{
    public class GameFileProviderTests
    {
        private readonly FakeArchiveProvider archiveProvider;
        private readonly MockFileSystem fs;
        private readonly GameFileProvider provider;
        private readonly ReadOnlyGameSettings settings;

        public GameFileProviderTests()
        {
            fs = new MockFileSystem();
            archiveProvider = new FakeArchiveProvider();
            settings = new ReadOnlyGameSettings(
                @"C:\game\data", archiveOrder: new[] { "archive3", "archive2", "archive1" });
            provider = new GameFileProvider(fs, settings, archiveProvider);
        }

        [Fact]
        public void WhenFileIsNowhere_Exists_IsFalse()
        {
            Assert.False(provider.Exists("file"));
        }

        [Fact]
        public void WhenFileIsInArchive_Exists_IsTrue()
        {
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", Array.Empty<byte>());

            Assert.True(provider.Exists("file"));
        }

        [Fact]
        public void WhenFileIsLoose_Exists_IsTrue()
        {
            fs.AddFile(@"C:\game\data\file", new MockFileData("q"));

            Assert.True(provider.Exists("file"));
        }

        [Fact]
        public void WhenFileIsNowhere_GetSize_ReturnsZero()
        {
            Assert.Equal(0U, provider.GetSize("file"));
        }

        [Fact]
        // The reason it is first, and not last, is that archive names are provided in priority order (last to first).
        public void WhenFileIsInArchives_GetSize_ReturnsFirstArchiveFileSize()
        {
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 1 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 1, 2, 3, 4 });
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2, 3, 4, 5, 6 });

            Assert.Equal(4U, provider.GetSize("file"));
        }

        [Fact]
        public void WhenFileIsLooseAndInArchives_GetSize_ReturnsLooseFileSize()
        {
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2 });
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 1, 2, 3, 4, 5 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 1, 2, 3 });
            fs.AddFile(@"C:\game\data\file", new MockFileData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));

            Assert.Equal(9U, provider.GetSize("file"));
        }

        [Fact]
        public void WhenFileIsNowhere_ReadBytes_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => provider.ReadBytes("file"));
        }

        [Fact]
        // The reason it is first, and not last, is that archive names are provided in priority order (last to first).
        public void WhenFileIsInArchives_ReadBytes_ReturnsFirstArchiveVersion()
        {
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 7, 8, 9 });
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2, 3 });

            Assert.Equal(new byte[] { 7, 8, 9 }, provider.ReadBytes("file").ToArray());
        }

        [Fact]
        public void WhenFileIsLooseAndInArchives_ReadBytes_ReturnsLooseVersion()
        {
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2, 3 });
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 7, 8, 9 });
            fs.AddFile(@"C:\game\data\file", new MockFileData(new byte[] { 12, 13, 14 }));

            Assert.Equal(new byte[] { 12, 13, 14 }, provider.ReadBytes("file").ToArray());
        }
        
        // Foregoing async tests because the GameFileProvider just inherits from
        // CascadingFileProvider. We're only interested in the file prioritization.
    }
}
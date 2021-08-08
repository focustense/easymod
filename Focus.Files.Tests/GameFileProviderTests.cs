using System;
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
                @"C:\game\data", archiveOrder: new[] { "archive1", "archive2", "archive3" });
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
        public void WhenFileIsNowhere_GetBytes_ReturnsNull()
        {
            Assert.False(provider.ReadBytes("file") != null);
        }

        [Fact]
        // The reason it is first, and not last, is that archive names are provided in priority order (last to first).
        public void WhenFileIsInArchives_GetBytes_ReturnsFirstArchiveVersion()
        {
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 7, 8, 9 });
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2, 3 });

            Assert.Equal(new byte[] { 7, 8, 9 }, provider.ReadBytes("file").ToArray());
        }

        [Fact]
        public void WhenFileIsLooseAndInArchives_GetBytes_ReturnsLooseVersion()
        {
            archiveProvider.AddFile(@"C:\game\data\archive1", "file", new byte[] { 1, 2, 3 });
            archiveProvider.AddFile(@"C:\game\data\archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile(@"C:\game\data\archive3", "file", new byte[] { 7, 8, 9 });
            fs.AddFile(@"C:\game\data\file", new MockFileData(new byte[] { 12, 13, 14 }));

            Assert.Equal(new byte[] { 12, 13, 14 }, provider.ReadBytes("file").ToArray());
        }
    }
}
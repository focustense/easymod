using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.Files.Tests
{
    public class GameFileProviderTests
    {
        private readonly FakeArchiveProvider archiveProvider;
        private readonly MockFileSystem fs;
        private readonly GameFileProvider provider;

        public GameFileProviderTests()
        {
            fs = new MockFileSystem();
            archiveProvider = new FakeArchiveProvider
            {
                LoadedArchivePaths = new[] { "archive1", "archive2", "archive3" }
            };
            provider = new GameFileProvider(fs, @"C:\game\data", archiveProvider);
        }

        [Fact]
        public void WhenFileIsNowhere_Exists_IsFalse()
        {
            Assert.False(provider.Exists("file"));
        }

        [Fact]
        public void WhenFileIsInArchive_Exists_IsTrue()
        {
            archiveProvider.AddFile("archive2", "file", Array.Empty<byte>());

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
            archiveProvider.AddFile("archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile("archive1", "file", new byte[] { 1, 2, 3 });
            archiveProvider.AddFile("archive3", "file", new byte[] { 7, 8, 9 });

            Assert.Equal(new byte[] { 1, 2, 3 }, provider.ReadBytes("file").ToArray());
        }

        [Fact]
        public void WhenFileIsLooseAndInArchives_GetBytes_ReturnsLooseVersion()
        {
            archiveProvider.AddFile("archive1", "file", new byte[] { 1, 2, 3 });
            archiveProvider.AddFile("archive2", "file", new byte[] { 4, 5, 6 });
            archiveProvider.AddFile("archive3", "file", new byte[] { 7, 8, 9 });
            fs.AddFile(@"C:\game\data\file", new MockFileData(new byte[] { 12, 13, 14 }));

            Assert.Equal(new byte[] { 12, 13, 14 }, provider.ReadBytes("file").ToArray());
        }
    }
}
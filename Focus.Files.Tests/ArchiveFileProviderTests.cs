using Focus.Testing.Files;
using System;
using Xunit;

namespace Focus.Files.Tests
{
    public class ArchiveFileProviderTests
    {
        private readonly FakeArchiveProvider archiveProvider;
        private readonly ArchiveFileProvider provider;

        public ArchiveFileProviderTests()
        {
            archiveProvider = new FakeArchiveProvider();
            provider = new ArchiveFileProvider(archiveProvider, "myarchive");
        }

        [Fact]
        public void WhenArchiveDoesNotContainFile_Exists_IsFalse()
        {
            Assert.False(provider.Exists("anything"));
        }

        [Fact]
        public void WhenArchiveContainsFile_Exists_IsTrue()
        {
            archiveProvider.AddFile("myarchive", "somefile", Array.Empty<byte>());

            Assert.True(provider.Exists("somefile"));
        }

        [Fact]
        public void GetSize_ReturnsSizeFromNamedArchive()
        {
            archiveProvider.AddFile("myarchive", "foo", new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            archiveProvider.AddFile("otherarchive", "bar", new byte[] { 1, 2, 3 });
            archiveProvider.AddFile("otherarchive", "baz", new byte[] { 1, 2 });
            archiveProvider.AddFile("myarchive", "bar", new byte[] { 1, 2, 3, 4, 5 });

            Assert.Equal(7U, provider.GetSize("foo"));
            Assert.Equal(5U, provider.GetSize("bar"));
            Assert.Equal(0U, provider.GetSize("baz"));
        }

        [Fact]
        public void ReadBytes_ReturnsDataFromNamedArchive()
        {
            archiveProvider.AddFile("myarchive", "foo", new byte[] { 1, 2 });
            archiveProvider.AddFile("otherarchive", "bar", new byte[] { 3, 4 });
            archiveProvider.AddFile("myarchive", "bar", new byte[] { 5, 6 });
            archiveProvider.AddFile("otherarchive", "baz", new byte[] { 7, 8 });

            Assert.Equal(new byte[] { 1, 2 }, provider.ReadBytes("foo").ToArray());
            Assert.Equal(new byte[] { 5, 6 }, provider.ReadBytes("bar").ToArray());
            Assert.False(provider.ReadBytes("baz") != null);
        }
    }
}

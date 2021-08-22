using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Focus.Files.Tests
{
    public class DirectoryFileProviderTests
    {
        private readonly MockFileSystem fileSystem;
        private readonly DirectoryFileProvider provider;

        public DirectoryFileProviderTests()
        {
            fileSystem = new MockFileSystem();
            provider = new(fileSystem, @"C:\Path\To\Root");
        }

        [Fact]
        public void WhenFileNotFoundAtRoot_Exists_IsFalse()
        {
            fileSystem.AddFile(@"C:\Other\Path\Dir\File.txt", new("zzz"));

            Assert.False(provider.Exists(@"Dir\File.txt"));
        }

        [Fact]
        public void WhenFileFoundAtRoot_Exists_IsTrue()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\Dir\File.txt", new("zzz"));

            Assert.True(provider.Exists(@"Dir\File.txt"));
        }

        [Fact]
        public void WhenFileNotFoundAtRoot_ReadBytes_Throws()
        {
            fileSystem.AddFile(@"C:\Other\file", new("zzz"));

            Assert.Throws<FileNotFoundException>(() => provider.ReadBytes("file"));
        }

        [Fact]
        public void WhenFileFoundAtRoot_ReadBytes_ReturnsFileData()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, provider.ReadBytes("file").ToArray());
        }
    }
}
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
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
        public async Task WhenFileNotFoundAtRoot_ExistsAsync_IsFalse()
        {
            fileSystem.AddFile(@"C:\Other\Path\Dir\File.txt", new("zzz"));

            Assert.False(await provider.ExistsAsync(@"Dir\File.txt"));
        }

        [Fact]
        public void WhenFileFoundAtRoot_Exists_IsTrue()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\Dir\File.txt", new("zzz"));

            Assert.True(provider.Exists(@"Dir\File.txt"));
        }

        [Fact]
        public async Task WhenFileFoundAtRoot_ExistsAsync_IsTrue()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\Dir\File.txt", new("zzz"));

            Assert.True(await provider.ExistsAsync(@"Dir\File.txt"));
        }

        [Fact]
        public void WhenFileNotFoundAtRoot_GetSize_ReturnsZero()
        {
            fileSystem.AddFile(@"C:\Other\file", new("zzz"));

            Assert.Equal(0U, provider.GetSize("file"));
        }

        [Fact]
        public async Task WhenFileNotFoundAtRoot_GetSizeAsync_ReturnsZero()
        {
            fileSystem.AddFile(@"C:\Other\file", new("zzz"));

            Assert.Equal(0U, await provider.GetSizeAsync("file"));
        }

        [Fact]
        public void WhenFileFoundAtRoot_GetSize_ReturnsFileSize()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            Assert.Equal(5U, provider.GetSize("file"));
        }

        [Fact]
        public async Task WhenFileFoundAtRoot_GetSizeAsync_ReturnsFileSize()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            Assert.Equal(5U, await provider.GetSizeAsync("file"));
        }

        [Fact]
        public void WhenFileNotFoundAtRoot_ReadBytes_Throws()
        {
            fileSystem.AddFile(@"C:\Other\file", new("zzz"));

            Assert.Throws<FileNotFoundException>(() => provider.ReadBytes("file"));
        }

        [Fact]
        public async Task WhenFileNotFoundAtRoot_ReadBytesAsync_Throws()
        {
            fileSystem.AddFile(@"C:\Other\file", new("zzz"));

            await Assert.ThrowsAsync<FileNotFoundException>(() => provider.ReadBytesAsync("file"));
        }

        [Fact]
        public void WhenFileFoundAtRoot_ReadBytes_ReturnsFileData()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, provider.ReadBytes("file").ToArray());
        }

        [Fact]
        public async Task WhenFileFoundAtRoot_ReadBytesAsync_ReturnsFileData()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            Assert.Equal(
                new byte[] { 1, 2, 3, 4, 5 }, (await provider.ReadBytesAsync("file")).ToArray());
        }

        [Fact]
        public async Task WhenFileNotFoundAtRoot_GetStreamAsync_Throws()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\otherfile", new("zzz"));

            await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetStreamAsync("file"));
        }

        [Fact]
        public async Task WhenFileFoundAtRoot_GetStreamAsync_ReturnsFileData()
        {
            fileSystem.AddFile(@"C:\Path\To\Root\file", new(new byte[] { 1, 2, 3, 4, 5 }));

            using var stream = await provider.GetStreamAsync("file");
            var data = new byte[stream.Length];
            await stream.ReadAsync(data);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, data);
        }
    }
}
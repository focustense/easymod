using Focus.Testing.Files;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Focus.Files.Tests
{
    public class CascadingFileProviderTests
    {
        private readonly FakeFileProvider innerProvider1;
        private readonly FakeFileProvider innerProvider2;
        private readonly CascadingFileProvider provider;

        public CascadingFileProviderTests()
        {
            innerProvider1 = new FakeFileProvider();
            innerProvider2 = new FakeFileProvider();
            provider = new CascadingFileProvider(new[] { innerProvider1, innerProvider2 });
        }

        [Fact]
        public void WhenNoProviderHasFile_Exists_IsFalse()
        {
            Assert.False(provider.Exists("foo"));
        }

        [Fact]
        public async Task WhenNoProviderHasFile_ExistsAsync_IsFalse()
        {
            Assert.False(await provider.ExistsAsync("foo"));
        }

        [Fact]
        public void WhenAnyProviderHasFile_Exists_IsTrue()
        {
            innerProvider1.PutFile("provider1_file", Array.Empty<byte>());
            innerProvider2.PutFile("provider2_file", Array.Empty<byte>());

            Assert.True(provider.Exists("provider1_file"));
            Assert.True(provider.Exists("provider2_file"));
        }

        [Fact]
        public async Task WhenAnyProviderHasFile_ExistsAsync_IsTrue()
        {
            innerProvider1.PutFile("provider1_file", Array.Empty<byte>());
            innerProvider2.PutFile("provider2_file", Array.Empty<byte>());

            Assert.True(await provider.ExistsAsync("provider1_file"));
            Assert.True(await provider.ExistsAsync("provider2_file"));
        }

        [Fact]
        public void WhenNoProviderHasFile_GetSize_ReturnsZero()
        {
            Assert.Equal(0U, provider.GetSize("foo"));
        }

        [Fact]
        public async Task WhenNoProviderHasFile_GetSizeAsync_ReturnsZero()
        {
            Assert.Equal(0U, await provider.GetSizeAsync("foo"));
        }

        [Fact]
        public void WhenSingleProviderHasFile_GetSize_ReturnsProviderFileSize()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3 });
            innerProvider2.PutFile("provider2_file", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            Assert.Equal(8U, provider.GetSize("provider2_file"));
        }

        [Fact]
        public async Task WhenSingleProviderHasFile_GetSizeAsync_ReturnsProviderFileSize()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3 });
            innerProvider2.PutFile("provider2_file", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            Assert.Equal(8U, await provider.GetSizeAsync("provider2_file"));
        }

        [Fact]
        public void WhenMultipleProvidersHaveFile_GetSize_ReturnsFirstFileSize()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3 });
            innerProvider2.PutFile("common", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            Assert.Equal(3U, provider.GetSize("common"));
        }

        [Fact]
        public async Task WhenMultipleProvidersHaveFile_GetSizeAsync_ReturnsFirstFileSize()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3 });
            innerProvider2.PutFile("common", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            Assert.Equal(3U, await provider.GetSizeAsync("common"));
        }

        [Fact]
        public void WhenNoProviderHasFile_ReadBytes_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => provider.ReadBytes("foo"));
        }

        [Fact]
        public async Task WhenNoProviderHasFile_ReadBytesAsync_Throws()
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() => provider.ReadBytesAsync("foo"));
        }

        [Fact]
        public void WhenSingleProviderHasFile_ReadBytes_ReturnsProviderFile()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("provider2_file", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(new byte[] { 5, 6, 7, 8 }, provider.ReadBytes("provider2_file").ToArray());
        }

        [Fact]
        public async Task WhenSingleProviderHasFile_ReadBytesAsync_ReturnsProviderFile()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("provider2_file", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(
                new byte[] { 5, 6, 7, 8 },
                (await provider.ReadBytesAsync("provider2_file")).ToArray());
        }

        [Fact]
        public void WhenMultipleProvidersHaveFile_ReadBytes_ReturnsFirstFile()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("common", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(new byte[] { 1, 2, 3, 4 }, provider.ReadBytes("common").ToArray());
        }

        [Fact]
        public async Task WhenMultipleProvidersHaveFile_ReadBytesAsync_ReturnsFirstFile()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("common", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(
                new byte[] { 1, 2, 3, 4 },
                (await provider.ReadBytesAsync("common")).ToArray());
        }

        [Fact]
        public async Task WhenNoProviderHasFile_GetStreamAsync_Throws()
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetStreamAsync("foo"));
        }

        [Fact]
        public async Task WhenSingleProviderHasFile_GetStreamAsync_ReturnsProviderStream()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("provider2_file", new byte[] { 5, 6, 7, 8 });

            using var stream = await provider.GetStreamAsync("provider2_file");
            var data = new byte[stream.Length];
            stream.Read(data);
            Assert.Equal(new byte[] { 5, 6, 7, 8 }, data);
        }

        [Fact]
        public async Task WhenMultipleProvidersHaveFile_GetStreamAsync_ReturnsFirstStream()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("common", new byte[] { 5, 6, 7, 8 });

            using var stream = await provider.GetStreamAsync("common");
            var data = new byte[stream.Length];
            stream.Read(data);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, data);
        }
    }
}

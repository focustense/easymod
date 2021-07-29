using System;
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
        public void WhenAnyProviderHasFile_Exists_IsTrue()
        {
            innerProvider1.PutFile("provider1_file", Array.Empty<byte>());
            innerProvider2.PutFile("provider2_file", Array.Empty<byte>());

            Assert.True(provider.Exists("provider1_file"));
            Assert.True(provider.Exists("provider2_file"));
        }

        [Fact]
        public void WhenNoProviderHasFile_ReadBytes_ReturnsNull()
        {
            // ReadOnlySpan doesn't play nice with xUnit's assertions.
            Assert.False(provider.ReadBytes("foo") != null);
        }

        [Fact]
        public void WhenSingleProviderHasFile_ReadBytes_ReturnsProviderFile()
        {
            innerProvider1.PutFile("provider1_file", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("provider2_file", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(new byte[] { 5, 6, 7, 8 }, provider.ReadBytes("provider2_file").ToArray());
        }

        [Fact]
        public void WhenMultipleProvidersHaveFile_ReadBytes_ReturnsFirstFile()
        {
            innerProvider1.PutFile("common", new byte[] { 1, 2, 3, 4 });
            innerProvider2.PutFile("common", new byte[] { 5, 6, 7, 8 });

            Assert.Equal(new byte[] { 1, 2, 3, 4 }, provider.ReadBytes("common").ToArray());
        }
    }
}

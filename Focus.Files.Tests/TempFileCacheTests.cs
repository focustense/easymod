using Focus.Testing.Files;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Focus.Files.Tests
{
    public class TempFileCacheTests
    {
        public class AfterTempFilesCreated
        {
            private readonly TempFileCache cache;
            private readonly FakeFileProvider fileProvider;
            private readonly MockFileSystem fs;
            private readonly string file1TempPath;
            private readonly string file2TempPath;
            private readonly string file3TempPath;

            public AfterTempFilesCreated()
            {
                fs = new MockFileSystem();
                cache = new TempFileCache(fs);

                fileProvider = new FakeFileProvider();
                fileProvider.PutFile("file1", new byte[] { 1, 2, 3 });
                fileProvider.PutFile("file2", new byte[] { 4, 5, 6 });
                fileProvider.PutFile("file3", new byte[] { 7, 8, 9 });

                file1TempPath = cache.GetTempPath(fileProvider, "file1");
                file2TempPath = cache.GetTempPath(fileProvider, "file2");
                file3TempPath = cache.GetTempPath(fileProvider, "file3");
            }

            [Fact]
            public void TempFilesHaveContentFromFileProvider()
            {
                Assert.Equal(new byte[] { 1, 2, 3 }, fs.File.ReadAllBytes(file1TempPath));
                Assert.Equal(new byte[] { 4, 5, 6 }, fs.File.ReadAllBytes(file2TempPath));
                Assert.Equal(new byte[] { 7, 8, 9 }, fs.File.ReadAllBytes(file3TempPath));
            }

            [Fact]
            public void GetTempFilePath_ForPreviousFileName_ReusesOldPath()
            {
                // Replacing the original file here proves that the file is actually cached, i.e. a second request for
                // it doesn't re-read it from the source.
                fileProvider.PutFile("file2", new byte[] { 11, 22, 33 });
                var newTempPath = cache.GetTempPath(fileProvider, "file2");

                Assert.Equal(newTempPath, file2TempPath);
                Assert.Equal(new byte[] { 4, 5, 6 }, fs.File.ReadAllBytes(file2TempPath));
            }

            [Fact]
            public void GetTempFilePath_ForNewFileName_CreatesNewFile()
            {
                fileProvider.PutFile("file4", new byte[] { 15, 30, 45 });
                var newTempPath = cache.GetTempPath(fileProvider, "file4");

                Assert.NotEqual(newTempPath, file1TempPath);
                Assert.NotEqual(newTempPath, file2TempPath);
                Assert.NotEqual(newTempPath, file3TempPath);
                Assert.Equal(new byte[] { 15, 30, 45 }, fs.File.ReadAllBytes(newTempPath));
            }

            [Fact]
            public void Purge_DeletesAllCachedFiles()
            {
                cache.Purge();

                Assert.Empty(fs.AllFiles);
            }

            [Fact]
            public void Dispose_DeletesAllCachedFiles()
            {
                cache.Dispose();

                Assert.Empty(fs.AllFiles);
            }
        }
    }
}

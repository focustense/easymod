using Focus.Testing.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace Focus.Files.Tests
{
    public class FileSystemIndexTests : IDisposable
    {
        private readonly FileSystemIndex index;
        private readonly MockFileSystem fs;
        private readonly FakeFileSystemWatcher watcher;
        private readonly List<FileEventArgs> addedEvents = new();
        private readonly List<BucketedFileEventArgs> addedToBucketEvents = new();
        private readonly List<FileEventArgs> removedEvents = new();
        private readonly List<BucketedFileEventArgs> removedFromBucketEvents = new();

        public FileSystemIndexTests()
        {
            var fakeFileSystemWatcherFactory = new FakeFileSystemWatcherFactory();
            fs = new MockFileSystemWithFakeWatcher(fakeFileSystemWatcherFactory);
            AddFiles(new[]
            {
                @"C:\temp\abc.txt",
                @"C:\path\to\root\filtered.dat",
                @"C:\path\to\root\inroot.txt",
                @"C:\path\to\root\sub1\filtered.foo",
                @"C:\path\to\root\sub1\filtered.bar",
                @"C:\path\to\root\sub1\insub1.txt",
                @"C:\path\to\root\sub1\insub1.log",
                @"C:\path\to\root\sub1\common\a.txt",
                @"C:\path\to\root\sub2\insub2.txt",
                @"C:\path\to\root\sub2\common\a.txt",
                @"C:\path\to\root\sub2\filtered.baz",
            });
            index = FileSystemIndex.Build(
                fs, @"C:\path\to\root", Bucketizers.TopLevelDirectory(), new[] { ".txt", ".log" });
            index.Added += (sender, e) => addedEvents.Add(e);
            index.AddedToBucket += (sender, e) => addedToBucketEvents.Add(e);
            index.Removed += (sender, e) => removedEvents.Add(e);
            index.RemovedFromBucket += (sender, e) => removedFromBucketEvents.Add(e);
            watcher = fakeFileSystemWatcherFactory.Watchers.Last();
        }

        public void Dispose()
        {
            index.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void WhenInitialized_IncludesExistingBuckets()
        {
            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x => AssertBucket(x, "", "inroot.txt"),
                x => AssertBucket(x, "sub1", @"common\a.txt", "insub1.log", "insub1.txt"),
                x => AssertBucket(x, "sub2", @"common\a.txt", "insub2.txt"));
            Assert.Equal(new[] { "", "sub1", "sub2" }, index.GetBucketNames().OrderBy(x => x));
            Assert.Equal(new[] { "inroot.txt" }, index.GetFilePaths(""));
            Assert.Equal(
                new[] { @"common\a.txt", "insub1.log", "insub1.txt" }, index.GetFilePaths("sub1").OrderBy(f => f));
            Assert.Equal(new[] { @"common\a.txt", "insub2.txt" }, index.GetFilePaths("sub2").OrderBy(f => f));
            Assert.True(index.Contains("", "inroot.txt"));
            Assert.True(index.Contains("sub1", "insub1.log"));
            Assert.True(index.Contains("sub1", "insub1.txt"));
            Assert.True(index.Contains("sub1", @"common\a.txt"));
            Assert.True(index.Contains("sub2", "insub2.txt"));
            Assert.True(index.Contains("sub2", @"common\a.txt"));
            Assert.False(index.Contains("", @"C:\temp\abc.txt"));
            Assert.False(index.Contains("", @"temp\abc.txt"));
            Assert.False(index.Contains("", @"abc.txt"));
            Assert.False(index.Contains("sub1", @"filtered.foo"));
            Assert.False(index.Contains("sub1", @"filtered.bar"));
            Assert.False(index.Contains("sub2", @"filtered.baz"));
            Assert.False(index.IsEmpty());
            Assert.False(index.IsEmpty(""));
            Assert.False(index.IsEmpty("sub1"));
            Assert.False(index.IsEmpty("sub2"));
            Assert.True(index.IsEmpty("unknown"));
        }

        [Fact]
        public void WhenInitialized_IncludesExistingFiles()
        {
            Assert.Equal(
                new[] {
                    "inroot.txt",
                    @"sub1\common\a.txt", @"sub1\insub1.log", @"sub1\insub1.txt",
                    @"sub2\common\a.txt", @"sub2\insub2.txt"
                },
                index.GetFilePaths().OrderBy(f => f));

            Assert.True(index.Contains("inroot.txt"));
            Assert.True(index.Contains(@"sub1\common\a.txt"));
            Assert.True(index.Contains(@"sub1\insub1.log"));
            Assert.True(index.Contains(@"sub1\insub1.txt"));
            Assert.True(index.Contains(@"sub2\common\a.txt"));
            Assert.True(index.Contains(@"sub2\insub2.txt"));
            Assert.False(index.Contains(@"C:\temp\abc.txt"));
            Assert.False(index.Contains(@"sub1\filtered.foo"));
            Assert.False(index.Contains(@"sub1\filtered.bar"));
            Assert.False(index.Contains(@"sub2\filtered.baz"));
        }

        [Fact]
        public void WhenInitialized_SetsUpWatcher()
        {
            Assert.Equal(@"C:\path\to\root", watcher.Path);
            Assert.Equal(new[] { "*.txt", "*.log" }, watcher.Filters);
            Assert.Equal(NotifyFilters.FileName, watcher.NotifyFilter);
            Assert.True(watcher.IncludeSubdirectories);
            Assert.True(watcher.EnableRaisingEvents);
        }

        [Theory]
        [InlineData("inroot.txt", "")]
        [InlineData("insub1.log", "sub1")]
        [InlineData("insub1.txt", "sub1")]
        [InlineData("insub2.txt", "sub2")]
        [InlineData(@"common\a.txt", "sub1", "sub2")]
        [InlineData(@"b\c.log")]
        public void GetContainingBucketNames_ProvidesAllBucketsWithFile(
            string pathInBucket, params string[] expectedBucketNames)
        {
            var results = index.FindInBuckets(pathInBucket);
            var assertions = expectedBucketNames.Select(bucketName => (Action<KeyValuePair<string, string>>)(x =>
            {
                Assert.Equal(bucketName, x.Key);
                Assert.Equal(pathInBucket, x.Value);
            }));
            Assert.Collection(results.OrderBy(x => x.Key).ThenBy(x => x.Value), assertions.ToArray());
        }

        [Fact]
        public void WhenFileAdded_IncludesNewFile()
        {
            watcher.RaiseCreated(@"sub2\new.txt");
            watcher.RaiseCreated(@"sub3\new.txt");

            Assert.Equal(
                new[] {
                    "inroot.txt",
                    @"sub1\common\a.txt",
                    @"sub1\insub1.log",
                    @"sub1\insub1.txt",
                    @"sub2\common\a.txt",
                    @"sub2\insub2.txt",
                    @"sub2\new.txt",
                    @"sub3\new.txt"
                },
                index.GetFilePaths().OrderBy(f => f));
            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x => AssertBucket(x, "", "inroot.txt"),
                x => AssertBucket(x, "sub1", @"common\a.txt", "insub1.log", "insub1.txt"),
                x => AssertBucket(x, "sub2", @"common\a.txt", "insub2.txt", "new.txt"),
                x => AssertBucket(x, "sub3", "new.txt"));
            Assert.Equal(new[] { "", "sub1", "sub2", "sub3" }, index.GetBucketNames().OrderBy(x => x));
            Assert.True(index.Contains(@"sub2\new.txt"));
            Assert.True(index.Contains(@"sub3\new.txt"));
            Assert.True(index.Contains("sub2", "new.txt"));
            Assert.True(index.Contains("sub3", "new.txt"));
            Assert.False(index.IsEmpty("sub3"));
        }

        [Fact]
        public void WhenFileAdded_FiresAddedEvents()
        {
            watcher.RaiseCreated(@"sub2\new.txt");
            watcher.RaiseCreated(@"sub3\new.txt");

            Assert.Collection(
                addedEvents,
                e => Assert.Equal(@"sub2\new.txt", e.Path),
                e => Assert.Equal(@"sub3\new.txt", e.Path));
            Assert.Collection(
                addedToBucketEvents,
                e =>
                {
                    Assert.Equal("sub2", e.BucketName);
                    Assert.Equal("new.txt", e.Path);
                },
                e =>
                {
                    Assert.Equal("sub3", e.BucketName);
                    Assert.Equal("new.txt", e.Path);
                });
        }

        [Fact]
        public void WhenFileDeleted_ForgetsOldFile()
        {
            watcher.RaiseDeleted(@"sub1\insub1.log");
            watcher.RaiseDeleted(@"sub2\common\a.txt");
            watcher.RaiseDeleted(@"blah\blah\blah.log"); // Does nothing, but shouldn't throw.

            Assert.Equal(
                new[] { "inroot.txt", @"sub1\common\a.txt", @"sub1\insub1.txt", @"sub2\insub2.txt" },
                index.GetFilePaths().OrderBy(f => f));
            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x => AssertBucket(x, "", "inroot.txt"),
                x => AssertBucket(x, "sub1", @"common\a.txt", "insub1.txt"),
                x => AssertBucket(x, "sub2", "insub2.txt"));
            Assert.Equal(new[] { "", "sub1", "sub2" }, index.GetBucketNames().OrderBy(x => x));
            Assert.False(index.Contains(@"sub1\insub1.log"));
            Assert.False(index.Contains(@"sub2\common\a.txt"));
            Assert.False(index.Contains("sub1", "insub1.log"));
            Assert.False(index.Contains("sub2", @"common\a.txt"));
        }

        [Fact]
        public void WhenFileDeleted_FiresRemovedEvents()
        {
            watcher.RaiseDeleted(@"sub1\insub1.log");
            watcher.RaiseDeleted(@"sub2\common\a.txt");
            watcher.RaiseDeleted(@"blah\blah\blah.log"); // Does nothing, but shouldn't fire events.

            Assert.Collection(
                removedEvents,
                e => Assert.Equal(@"sub1\insub1.log", e.Path),
                e => Assert.Equal(@"sub2\common\a.txt", e.Path));
            Assert.Collection(
                removedFromBucketEvents,
                e =>
                {
                    Assert.Equal("sub1", e.BucketName);
                    Assert.Equal("insub1.log", e.Path);
                },
                e =>
                {
                    Assert.Equal("sub2", e.BucketName);
                    Assert.Equal(@"common\a.txt", e.Path);
                });
        }

        [Fact]
        public void WhenFileRenamed_TracksNewName()
        {
            watcher.RaiseRenamed("inroot.txt", "newinroot.txt");
            watcher.RaiseRenamed(@"sub2\insub2.txt", @"sub3\insub3.log");

            Assert.Equal(
                new[] { "newinroot.txt", @"sub1\common\a.txt", @"sub1\insub1.log", @"sub1\insub1.txt", @"sub2\common\a.txt", @"sub3\insub3.log" },
                index.GetFilePaths().OrderBy(f => f));
            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x => AssertBucket(x, "", "newinroot.txt"),
                x => AssertBucket(x, "sub1", @"common\a.txt", "insub1.log", "insub1.txt"),
                x => AssertBucket(x, "sub2", @"common\a.txt"),
                x => AssertBucket(x, "sub3", "insub3.log"));
            Assert.Equal(new[] { "", "sub1", "sub2", "sub3" }, index.GetBucketNames().OrderBy(x => x));
            Assert.True(index.Contains("newinroot.txt"));
            Assert.True(index.Contains(@"sub3\insub3.log"));
            Assert.True(index.Contains("", "newinroot.txt"));
            Assert.True(index.Contains("sub3", "insub3.log"));
            Assert.False(index.Contains("inroot.txt"));
            Assert.False(index.Contains(@"sub2\insub2.txt"));
            Assert.False(index.Contains("", "inroot.txt"));
            Assert.False(index.Contains("sub2", "insub2.txt"));
            Assert.False(index.IsEmpty("sub3"));
        }

        [Fact]
        public void WhenFileRenamed_FiresAddedAndRemovedEvents()
        {
            watcher.RaiseRenamed("inroot.txt", "newinroot.txt");
            watcher.RaiseRenamed(@"sub2\insub2.txt", @"sub3\insub3.log");

            Assert.Collection(
                addedEvents,
                e => Assert.Equal("newinroot.txt", e.Path),
                e => Assert.Equal(@"sub3\insub3.log", e.Path));
            Assert.Collection(
                addedToBucketEvents,
                e =>
                {
                    Assert.Equal("", e.BucketName);
                    Assert.Equal("newinroot.txt", e.Path);
                },
                e =>
                {
                    Assert.Equal("sub3", e.BucketName);
                    Assert.Equal("insub3.log", e.Path);
                });
            Assert.Collection(
                removedEvents,
                e => Assert.Equal("inroot.txt", e.Path),
                e => Assert.Equal(@"sub2\insub2.txt", e.Path));
            Assert.Collection(
                removedFromBucketEvents,
                e =>
                {
                    Assert.Equal("", e.BucketName);
                    Assert.Equal("inroot.txt", e.Path);
                },
                e =>
                {
                    Assert.Equal("sub2", e.BucketName);
                    Assert.Equal("insub2.txt", e.Path);
                });
        }

        [Fact]
        public void WhenDirectoryFullyEmptied_BucketIsEmpty()
        {
            watcher.RaiseDeleted(@"sub1\insub1.log");
            watcher.RaiseDeleted(@"sub1\insub1.txt");
            watcher.RaiseDeleted(@"sub1\common\a.txt");

            Assert.True(index.IsEmpty("sub1"));
        }

        [Fact]
        public void WhenWatchingPaused_StopsRaisingEvents()
        {
            index.PauseWatching();

            Assert.False(watcher.EnableRaisingEvents);
        }

        [Fact]
        public void WhenWatchingResumed_StartsRaisingEvents()
        {
            index.PauseWatching();
            index.ResumeWatching();

            Assert.True(watcher.EnableRaisingEvents);
        }

        private void AddFiles(IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames)
                fs.AddFile(fileName, new MockFileData(""));
        }

        private static void AssertBucket(
            KeyValuePair<string, IEnumerable<string>> bucket, string name, params string[] values)
        {
            Assert.Equal(name, bucket.Key);
            Assert.Equal(values.OrderBy(f => f), bucket.Value.OrderBy(f => f));
        }
    }

    class MockFileSystemWithFakeWatcher : MockFileSystem
    {
        public override IFileSystemWatcherFactory FileSystemWatcher { get; }

        public MockFileSystemWithFakeWatcher(IFileSystemWatcherFactory watcherFactory)
        {
            FileSystemWatcher = watcherFactory;
        }
    }
}

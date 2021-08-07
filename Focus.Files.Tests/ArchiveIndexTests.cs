using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.Files.Tests
{
    public class ArchiveIndexTests
    {
        private readonly FakeArchiveProvider archiveProvider;
        private readonly ArchiveIndex index;

        public ArchiveIndexTests()
        {
            archiveProvider = new FakeArchiveProvider();
            index = new ArchiveIndex(archiveProvider);
        }

        [Fact]
        public void IsInitiallyEmpty()
        {
            Assert.Equal(Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>(), index.GetBucketedFilePaths());
            Assert.Equal(Enumerable.Empty<string>(), index.GetFilePaths("any_bucket"));
            Assert.Equal(Enumerable.Empty<KeyValuePair<string, string>>(), index.FindInBuckets(@"any\path"));
        }

        [Fact]
        public void WhenArchivesAdded_IncludesFilesFromArchives()
        {
            archiveProvider.AddFiles(
                @"path\to\archive1",
                @"top1", @"main\file1", @"main\sub\file2", @"main\sub\file3");
            archiveProvider.AddFiles(
                @"path\to\archive2",
                @"top1", @"top2", @"main\file1", @"main\sub\file2", @"main\sub\file4");
            archiveProvider.AddFiles(
                @"path\to\archive3",
                @"alt\file1", @"alt\sub\file2", @"alt\sub\file3");
            index.AddArchives(new[] { @"path\to\archive1", @"path\to\archive2", @"path\to\archive3" });

            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x =>
                {
                    Assert.Equal(@"path\to\archive1", x.Key);
                    Assert.Equal(
                        new[] { @"main\file1", @"main\sub\file2", @"main\sub\file3", "top1" },
                        x.Value.OrderBy(f => f));
                },
                x =>
                {
                    Assert.Equal(@"path\to\archive2", x.Key);
                    Assert.Equal(
                        new[] { @"main\file1", @"main\sub\file2", @"main\sub\file4", "top1", "top2" },
                        x.Value.OrderBy(f => f));
                },
                x =>
                {
                    Assert.Equal(@"path\to\archive3", x.Key);
                    Assert.Equal(
                        new[] { @"alt\file1", @"alt\sub\file2", @"alt\sub\file3" },
                        x.Value.OrderBy(f => f));
                });
            Assert.Equal(
                new[] { @"main\file1", @"main\sub\file2", @"main\sub\file3", "top1" },
                index.GetFilePaths(@"Path\To\Archive1").OrderBy(f => f));
            Assert.Equal(
                new[] { @"main\file1", @"main\sub\file2", @"main\sub\file4", "top1", "top2" },
                index.GetFilePaths(@"path/to\ARCHIVE2").OrderBy(f => f));
            Assert.Equal(
                new[] { @"alt\file1", @"alt\sub\file2", @"alt\sub\file3" },
                index.GetFilePaths(@"pAtH\tO\aRcHiVe3").OrderBy(f => f));
            Assert.Empty(index.GetFilePaths(@"unknown\archive"));
        }

        [Fact]
        public void WhenArchivesAdded_FindsContentPaths()
        {
            archiveProvider.AddFiles(
                @"path\to\archive1",
                @"top1", @"main\file1", @"main\sub\file2", @"main\sub\file3");
            archiveProvider.AddFiles(
                @"path\to\archive2",
                @"top1", @"top2", @"main\file1", @"main\sub\file2", @"main\sub\file4");
            archiveProvider.AddFiles(
                @"path\to\archive3",
                @"alt\file1", @"alt\sub\file2", @"alt\sub\file3");
            index.AddArchives(new[] { @"path\to\archive1", @"path\to\archive2", @"path\to\archive3" });

            Assert.Collection(
                index.FindInBuckets("Top1").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", "top1"),
                x => AssertKeyValuePair(x, @"path\to\archive2", "top1"));
            Assert.Collection(
                index.FindInBuckets("top2").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive2", "top2"));
            Assert.Collection(
                index.FindInBuckets(@"MAIN\FILE1").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", @"main\file1"),
                x => AssertKeyValuePair(x, @"path\to\archive2", @"main\file1"));
            Assert.Collection(
                index.FindInBuckets(@"maiN\sub\fIle2").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", @"main\sub\file2"),
                x => AssertKeyValuePair(x, @"path\to\archive2", @"main\sub\file2"));
            Assert.Collection(
                index.FindInBuckets(@"main/sub/file3").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", @"main\sub\file3"));
            Assert.Collection(
                index.FindInBuckets(@"MaIn\SuB\fILE4").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive2", @"main\sub\file4"));
            Assert.Collection(
                index.FindInBuckets(@"alt\file1").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive3", @"alt\file1"));
            Assert.Collection(
                index.FindInBuckets(@"alt\sub\file2").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive3", @"alt\sub\file2"));
            Assert.Collection(
                index.FindInBuckets(@"alt\sub\file3").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive3", @"alt\sub\file3"));
        }

        [Fact]
        public void WhenArchiveRemoved_ForgetsFilesFromArchive()
        {
            archiveProvider.AddFiles(
                @"path\to\archive1",
                @"top1", @"main\file1", @"main\sub\file2", @"main\sub\file3");
            archiveProvider.AddFiles(
                @"path\to\archive2",
                @"top1", @"top2", @"main\file1", @"main\sub\file2", @"main\sub\file4");
            archiveProvider.AddFiles(
                @"path\to\archive3",
                @"alt\file1", @"alt\sub\file2", @"alt\sub\file3");
            index.AddArchives(new[] { @"path\to\archive1", @"path\to\archive2", @"path\to\archive3" });
            index.RemoveArchive(@"path\to\archive2");

            Assert.Collection(
                index.GetBucketedFilePaths().OrderBy(x => x.Key),
                x =>
                {
                    Assert.Equal(@"path\to\archive1", x.Key);
                    Assert.Equal(
                        new[] { @"main\file1", @"main\sub\file2", @"main\sub\file3", "top1" },
                        x.Value.OrderBy(f => f));
                },
                x =>
                {
                    Assert.Equal(@"path\to\archive3", x.Key);
                    Assert.Equal(
                        new[] { @"alt\file1", @"alt\sub\file2", @"alt\sub\file3" },
                        x.Value.OrderBy(f => f));
                });
            Assert.Empty(index.GetFilePaths(@"path\to\archive2").OrderBy(f => f));
            Assert.Collection(
                index.FindInBuckets("Top1").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", "top1"));
            Assert.Empty(index.FindInBuckets("top2").OrderBy(x => x.Key));
            Assert.Collection(
                index.FindInBuckets(@"MAIN\FILE1").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", @"main\file1"));
            Assert.Collection(
                index.FindInBuckets(@"maiN\sub\fIle2").OrderBy(x => x.Key),
                x => AssertKeyValuePair(x, @"path\to\archive1", @"main\sub\file2"));
            Assert.Empty(index.FindInBuckets(@"MaIn\SuB\fILE4").OrderBy(x => x.Key));
        }

        private static void AssertKeyValuePair(KeyValuePair<string, string> pair, string expectedKey, string expectedValue)
        {
            Assert.Equal(expectedKey, pair.Key);
            Assert.Equal(expectedValue, pair.Value);
        }
    }
}

using System.Linq;
using Xunit;

namespace Focus.Files.Tests
{
    public class ReadOnlyFileIndexTests
    {
        private readonly ReadOnlyFileIndex index;

        public ReadOnlyFileIndexTests()
        {
            index = new(new[]
            {
                @"a\b\c\file1",
                @"a\b\c\file2",
                @"a\b\c\file3",
                @"a\b\c\file4",
                @"a\b\c\file5",
            });
        }

        [Fact]
        public void GetFilePaths_ReturnsAllPaths()
        {
            Assert.Equal(
                new[]
                {
                    @"a\b\c\file1",
                    @"a\b\c\file2",
                    @"a\b\c\file3",
                    @"a\b\c\file4",
                    @"a\b\c\file5",
                },
                index.GetFilePaths());
        }

        [Fact]
        public void Contains_ReturnsTrueForIncludedFiles()
        {
            Assert.True(index.Contains(@"a\b\c\file1"));
            Assert.True(index.Contains(@"a\b\c\FiLe2"));
            Assert.True(index.Contains(@"a\b\c\file3"));
            Assert.True(index.Contains(@"a\B\c\file4"));
            Assert.True(index.Contains(@"A\b\c\FILE5"));

            Assert.False(index.Contains("file1"));
            Assert.False(index.Contains(@"a\b\c\d\file1"));
            Assert.False(index.Contains(@"a\b\c\other"));
        }

        [Fact]
        public void IsEmpty_IsFalseForNonEmptyIndex()
        {
            Assert.False(index.IsEmpty());
            Assert.True(new ReadOnlyFileIndex(Enumerable.Empty<string>()).IsEmpty());
        }
    }
}

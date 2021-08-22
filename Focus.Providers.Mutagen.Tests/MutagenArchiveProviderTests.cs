using Focus.Files;
using Moq;
using Mutagen.Bethesda;
using Serilog;
using System;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests
{
    public class MutagenArchiveProviderTests
    {
        private static readonly GameRelease GameRelease = GameRelease.SkyrimSE;

        private readonly Mock<IArchiveStatics> archiveMock;
        private readonly ILogger logger;
        private readonly MutagenArchiveProvider provider;

        public MutagenArchiveProviderTests()
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            archiveMock = new Mock<IArchiveStatics>();
            var gameSelection = new GameSelection(GameRelease);
            provider = new MutagenArchiveProvider(archiveMock.Object, gameSelection, logger);
        }

        [Fact]
        public void WhenFileIsInArchive_ContainsFile_ReturnsTrue()
        {
            archiveMock.Setup(x => x.CreateReader(GameRelease, "foo.bsa")).Returns(new FakeArchiveReader()
                .Put(@"a\b\c\1", "")
                .Put(@"a\b\c\2", "")
                .Put(@"a\b\c\3", ""));
            archiveMock.Setup(x => x.CreateReader(GameRelease, "bar.bsa")).Returns(new FakeArchiveReader());

            Assert.True(provider.ContainsFile("foo.bsa", @"a\b\c\1"));
            Assert.True(provider.ContainsFile("foo.bsa", @"a\b\c\2"));
            Assert.True(provider.ContainsFile("foo.bsa", @"a\b\c\3"));

            Assert.False(provider.ContainsFile("bar.bsa", @"a\b\c\1"));
            Assert.False(provider.ContainsFile("foo.bsa", @"a\b\c"));
            Assert.False(provider.ContainsFile("foo.bsa", @"b\c\1"));
            Assert.False(provider.ContainsFile("foo.bsa", @"1"));
        }

        [Fact]
        public void GetArchiveFileNames_ReturnsFilesWithPathPrefix()
        {
            archiveMock.Setup(x => x.CreateReader(GameRelease, "foo.bsa")).Returns(new FakeArchiveReader()
                .Put(@"a\1", "")
                .Put(@"a\b\2", "")
                .Put(@"a\b\c\3", "")
                .Put(@"a\b\c\4", "")
                .Put(@"d\5", "")
                .Put(@"d\6", "")
                .Put(@"d\e\7", "")
                .Put(@"d\e\f\8", ""));

            Assert.Equal(
                new[] { @"a\1", @"a\b\2", @"a\b\c\3", @"a\b\c\4", @"d\5", @"d\6", @"d\e\7", @"d\e\f\8" },
                provider.GetArchiveFileNames("foo.bsa"));
            Assert.Equal(
                new[] { @"a\1", @"a\b\2", @"a\b\c\3", @"a\b\c\4" },
                provider.GetArchiveFileNames("foo.bsa", @"a"));
            Assert.Equal(new[] { @"a\b\2", @"a\b\c\3", @"a\b\c\4" }, provider.GetArchiveFileNames("foo.bsa", @"a\B\"));
            Assert.Equal(new[] { @"a\b\c\3", @"a\b\c\4" }, provider.GetArchiveFileNames("foo.bsa", @"A\b/C"));
            Assert.Equal(new[] { @"d\5", @"d\6", @"d\e\7", @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"d/"));
            Assert.Equal(new[] { @"d\e\7", @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"d/e"));
            Assert.Equal(new[] { @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"D\E\F\"));
        }

        [Fact]
        public void ReadBytes_ForDirectorylessPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => provider.ReadBytes(@"C:\game\data\dummy.bsa", "rootfile.txt"));
        }

        [Fact]
        public void ReadBytes_ForMissingFolder_Throws()
        {
            archiveMock
                .Setup(x => x.CreateReader(GameRelease, @"C:\game\data\archive.bsa"))
                .Returns(new FakeArchiveReader());
            Assert.Throws<ArchiveException>(() => provider.ReadBytes(@"C:\game\data\archive.bsa", @"unknown\foo.bar"));
        }

        [Fact]
        public void ReadBytes_ForMissingFile_Throws()
        {
            archiveMock
                .Setup(x => x.CreateReader(GameRelease, @"C:\game\data\archive.bsa"))
                .Returns(new FakeArchiveReader()
                    .Put(@"a\b\c\1", "")
                    .Put(@"a\b\c\2", "")
                    .Put(@"a\b\c\3", ""));

            Assert.Throws<ArchiveException>(() => provider.ReadBytes(@"C:\game\data\archive.bsa", @"a\b\c\4"));
        }

        [Fact]
        public void ReadBytes_ForFoundFile_ReturnsContents()
        {
            archiveMock
                .Setup(x => x.CreateReader(GameRelease, @"C:\game\data\archive.bsa"))
                .Returns(new FakeArchiveReader()
                    .Put(@"a\b\c\1", "")
                    .Put(@"a\b\c\2", new byte[] { 1, 2, 3, 4, 5 })
                    .Put(@"a\b\c\3", ""));
            var data = provider.ReadBytes(@"C:\game\data\archive.bsa", @"a\b\c\2");

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, data.ToArray());
        }

        [Fact]
        public void WhenArchiveHasInvalidData_FlagsAsBad()
        {
            archiveMock.Setup(x => x.CreateReader(GameRelease, "foo.bsa")).Returns(new FakeArchiveReader()
                .Put(@"a\b\c\1", "")
                .MarkFolderCorrupted(@"a\b\c"));

            Assert.False(provider.ContainsFile("foo.bsa", @"a\b\c\1"));
            Assert.Equal(Enumerable.Empty<string>(), provider.GetArchiveFileNames("foo.bsa"));
            Assert.Contains("foo.bsa", provider.GetBadArchivePaths());
        }
    }
}

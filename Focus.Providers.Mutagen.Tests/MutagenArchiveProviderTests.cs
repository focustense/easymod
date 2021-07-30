using Focus.Files;
using Moq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests
{
    public class MutagenArchiveProviderTests
    {
        private static readonly GameRelease GameRelease = GameRelease.SkyrimSE;

        private readonly Mock<IArchiveStatics> archiveMock;
        private readonly Mock<IReadOnlyGameEnvironment<ISkyrimModGetter>> environmentMock;
        private readonly Mock<ILoadOrder<IModListing<ISkyrimModGetter>>> loadOrderMock;
        private readonly ILogger logger;
        private readonly MutagenArchiveProvider provider;

        public MutagenArchiveProviderTests()
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            archiveMock = new Mock<IArchiveStatics>();
            archiveMock.Setup(x => x.GetIniListings(GameRelease)).Returns(new[]
            {
                new FileName("ini1.bsa"),
                new FileName("ini2.bsa"),
            });
            environmentMock = new Mock<IReadOnlyGameEnvironment<ISkyrimModGetter>>();
            loadOrderMock = new Mock<ILoadOrder<IModListing<ISkyrimModGetter>>>();
            loadOrderMock.Setup(x => x.ListedOrder).Returns(new[]
            {
                ModListing("plugin1.esp"),
                ModListing("plugin2.esp"),
                ModListing("plugin3.esp"),
            });
            environmentMock.SetupGet(x => x.LoadOrder).Returns(loadOrderMock.Object);
            provider = new MutagenArchiveProvider(environmentMock.Object, archiveMock.Object, GameRelease, logger);
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
            Assert.Equal(new[] { @"a\b\2", @"a\b\c\3", @"a\b\c\4" }, provider.GetArchiveFileNames("foo.bsa", @"a\b\"));
            Assert.Equal(new[] { @"a\b\c\3", @"a\b\c\4" }, provider.GetArchiveFileNames("foo.bsa", @"a\b/c"));
            Assert.Equal(new[] { @"d\5", @"d\6", @"d\e\7", @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"d/"));
            Assert.Equal(new[] { @"d\e\7", @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"d/e"));
            Assert.Equal(new[] { @"d\e\f\8" }, provider.GetArchiveFileNames("foo.bsa", @"d\e\f\"));
        }

        [Fact]
        public void GetArchivePath_ReturnsArchiveInDataDirectory()
        {
            environmentMock.SetupGet(x => x.DataFolderPath).Returns(@"C:\Games\Skyrim\Data");

            Assert.Equal(@"C:\Games\Skyrim\Data\Foo.bsa", provider.GetArchivePath("Foo.bsa"));
        }

        [Fact]
        public void GetLoadedArchivePaths_GetsFromArchiveService_UsingListingOrder()
        {
            const string gameDataPath = @"C:\game\data";
            environmentMock.SetupGet(x => x.DataFolderPath).Returns(gameDataPath);
            archiveMock
                .Setup(x => x.GetApplicableArchivePaths(
                    GameRelease, gameDataPath, It.IsAny<IEnumerable<FileName>>()))
                .Returns(new[]
                {
                    new FilePath(@"C:\game\data\dummy1.bsa"),
                    new FilePath(@"C:\game\data\dummy2.bsa"),
                });
            var loadedArchivePaths = provider.GetLoadedArchivePaths();

            Assert.Equal(
                new[] { @"C:\game\data\dummy1.bsa", @"C:\game\data\dummy2.bsa" }, loadedArchivePaths);
            var expectedOrdering = new[] {
                new FileName("ini1.bsa"),
                new FileName("ini2.bsa"),
                new FileName("plugin1.bsa"),
                new FileName("plugin1 - Textures.bsa"),
                new FileName("plugin2.bsa"),
                new FileName("plugin2 - Textures.bsa"),
                new FileName("plugin3.bsa"),
                new FileName("plugin3 - Textures.bsa"),
            };
            archiveMock.Verify(x => x.GetApplicableArchivePaths(GameRelease, @"C:\game\data", expectedOrdering));
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

        private static IModListing<ISkyrimModGetter> ModListing(string pluginName)
        {
            var listingMock = new Mock<IModListing<ISkyrimModGetter>>();
            listingMock.Setup(x => x.ModKey).Returns(ModKey.FromNameAndExtension(pluginName));
            return listingMock.Object;
        }
    }
}

using Moq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests
{
    public class GameSettingsTest
    {
        private const string GameDataPath = @"C:\game\data";

        private static readonly GameRelease GameRelease = GameRelease.SkyrimSE;

        private readonly Mock<IArchiveStatics> archiveMock;
        private readonly Mock<IReadOnlyGameEnvironment<ISkyrimModGetter>> environmentMock;
        private readonly Mock<ILoadOrder<IModListing<ISkyrimModGetter>>> loadOrderMock;
        private readonly GameSettings<ISkyrimModGetter> settings;

        public GameSettingsTest()
        {
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
            environmentMock.SetupGet(x => x.DataFolderPath).Returns(GameDataPath);
            environmentMock.SetupGet(x => x.LoadOrder).Returns(loadOrderMock.Object);
            var gameSelection = new GameSelection(GameRelease);
            settings = new GameSettings<ISkyrimModGetter>(environmentMock.Object, archiveMock.Object, gameSelection);
        }

        [Fact]
        public void ArchiveOrder_GetsFromArchiveService_UsingListingOrder()
        {
            const string gameDataPath = @"C:\game\data";
            
            archiveMock
                .Setup(x => x.GetApplicableArchivePaths(
                    GameRelease, gameDataPath, It.IsAny<IEnumerable<FileName>>()))
                .Returns(new[]
                {
                    new FilePath(@"C:\game\data\dummy1.bsa"),
                    new FilePath(@"C:\game\data\dummy2.bsa"),
                });
            var archiveOrder = settings.ArchiveOrder.ToList();

            Assert.Equal(new[] { "dummy1.bsa", "dummy2.bsa" }, archiveOrder);
            var expectedOrdering = new[] {
                new FileName(@"ini1.bsa"),
                new FileName(@"ini2.bsa"),
                new FileName(@"plugin1.bsa"),
                new FileName(@"plugin1 - Textures.bsa"),
                new FileName(@"plugin2.bsa"),
                new FileName(@"plugin2 - Textures.bsa"),
                new FileName(@"plugin3.bsa"),
                new FileName(@"plugin3 - Textures.bsa"),
            };
            archiveMock.Verify(x => x.GetApplicableArchivePaths(GameRelease, @"C:\game\data", expectedOrdering));
        }

        [Fact]
        public void DataDirectory_ReturnsDataDirectoryFromEnvironment()
        {
            Assert.Equal(GameDataPath, settings.DataDirectory);
        }

        [Fact]
        public void PluginLoadOrder_ReturnsListedOrder()
        {
            Assert.Equal(new[] { "plugin1.esp", "plugin2.esp", "plugin3.esp" }, settings.PluginLoadOrder);
        }

        private static IModListing<ISkyrimModGetter> ModListing(string pluginName)
        {
            var listingMock = new Mock<IModListing<ISkyrimModGetter>>();
            listingMock.Setup(x => x.ModKey).Returns(ModKey.FromNameAndExtension(pluginName));
            return listingMock.Object;
        }
    }
}

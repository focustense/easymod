using Focus.Environment;
using Moq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests
{
    public class GameSetupTests
    {
        private const string DataDirectory = @"C:\path\to\game";

        private readonly MockFileSystem fs;
        private readonly GameInstance game;
        private readonly List<IModListingGetter> listings = new();
        private readonly ILogger log;
        private readonly GameSetup setup;
        private readonly Mock<ISetupStatics> setupStaticsMock;

        public GameSetupTests()
        {
            log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            fs = new MockFileSystem();
            fs.Directory.CreateDirectory(DataDirectory);
            game = new GameInstance(GameRelease.SkyrimVR, DataDirectory);
            setupStaticsMock = new Mock<ISetupStatics>();
            setupStaticsMock.Setup(x => x.GetBaseMasters(It.IsAny<GameRelease>())).Returns(new List<ModKey>());
            setupStaticsMock
                .Setup(x => x.GetLoadOrderListings(game.GameRelease, DataDirectory, It.IsAny<bool>()))
                .Returns(listings);
            setup = new GameSetup(fs, setupStaticsMock.Object, game, log);
        }

        [Fact]
        public void InitialState_IsEmpty()
        {
            Assert.Empty(setup.AvailablePlugins);
            Assert.Equal(@"C:\path\to\game", setup.DataDirectory);
            Assert.IsType<NullLoadOrderGraph>(setup.LoadOrderGraph);
        }

        [Fact]
        public void WhenDetected_ProvidesPlugins()
        {
            CreateDummyMod("base1.esm", Enumerable.Empty<string>());
            CreateDummyMod("base2.esm", new[] { "base1.esm" });
            CreateDummyMod("plugin1.esp", new[] { "base1.esm" });
            CreateDummyMod("plugin2.esp", new[] { "base1.esm", "base2.esm" });
            CreateDummyMod("plugin3.esp", new[] { "base1.esm", "plugin1.esp" });
            listings.AddRange(
                CreateDummyListings("base1.esm", "base2.esm", "plugin1.esp", "plugin2.esp", "plugin3.esp"));
            setup.Detect(new HashSet<string>());

            Assert.Collection(
                setup.AvailablePlugins,
                x =>
                {
                    Assert.Equal("base1.esm", x.FileName);
                    Assert.True(x.IsEnabled);
                    Assert.True(x.IsReadable);
                    Assert.Empty(x.Masters);
                },
                x =>
                {
                    Assert.Equal("base2.esm", x.FileName);
                    Assert.True(x.IsEnabled);
                    Assert.True(x.IsReadable);
                    Assert.Equal(new[] { "base1.esm" }, x.Masters);
                },
                x =>
                {
                    Assert.Equal("plugin1.esp", x.FileName);
                    Assert.True(x.IsEnabled);
                    Assert.True(x.IsReadable);
                    Assert.Equal(new[] { "base1.esm" }, x.Masters);
                },
                x =>
                {
                    Assert.Equal("plugin2.esp", x.FileName);
                    Assert.True(x.IsEnabled);
                    Assert.True(x.IsReadable);
                    Assert.Equal(new[] { "base1.esm", "base2.esm" }, x.Masters);
                },
                x =>
                {
                    Assert.Equal("plugin3.esp", x.FileName);
                    Assert.True(x.IsEnabled);
                    Assert.True(x.IsReadable);
                    Assert.Equal(new[] { "base1.esm", "plugin1.esp" }, x.Masters);
                });
        }

        [Fact]
        public void WhenImplicitsIncludesPlugin_MarksAsImplicit()
        {
            CreateDummyMod("base1.esm", Enumerable.Empty<string>());
            CreateDummyMod("base2.esp", Enumerable.Empty<string>());
            CreateDummyMod("plugin1.esm", Enumerable.Empty<string>());
            CreateDummyMod("plugin2.esp", Enumerable.Empty<string>());
            listings.AddRange(CreateDummyListings("base1.esm", "base2.esp", "plugin1.esm", "plugin2.esp"));
            setupStaticsMock.Setup(x => x.GetBaseMasters(game.GameRelease)).Returns(new[]
            {
                ModKey.FromNameAndExtension("base1.esm"),
                ModKey.FromNameAndExtension("base2.esp"),
            });
            setup.Detect(new HashSet<string>());

            Assert.Collection(
                setup.AvailablePlugins,
                x =>
                {
                    Assert.Equal("base1.esm", x.FileName);
                    Assert.True(x.IsImplicit);
                },
                x =>
                {
                    Assert.Equal("base2.esp", x.FileName);
                    Assert.True(x.IsImplicit);
                },
                x =>
                {
                    Assert.Equal("plugin1.esm", x.FileName);
                    Assert.False(x.IsImplicit);
                },
                x =>
                {
                    Assert.Equal("plugin2.esp", x.FileName);
                    Assert.False(x.IsImplicit);
                });
        }

        [Fact]
        public void WhenListingIsDisabled_MarksAsDisabled()
        {
            CreateDummyMod("plugin1.esp", Enumerable.Empty<string>());
            CreateDummyMod("plugin2.esp", Enumerable.Empty<string>());
            CreateDummyMod("plugin3.esp", Enumerable.Empty<string>());
            listings.AddRange(new[]
            {
                CreateDummyListing("plugin1.esp", false),
                CreateDummyListing("plugin2.esp", true),
                CreateDummyListing("plugin3.esp", false),
            });
            setup.Detect(new HashSet<string>());

            Assert.Collection(
                setup.AvailablePlugins,
                x =>
                {
                    Assert.Equal("plugin1.esp", x.FileName);
                    Assert.False(x.IsEnabled);
                },
                x =>
                {
                    Assert.Equal("plugin2.esp", x.FileName);
                    Assert.True(x.IsEnabled);
                },
                x =>
                {
                    Assert.Equal("plugin3.esp", x.FileName);
                    Assert.False(x.IsEnabled);
                });
        }

        [Fact]
        public void WhenPluginIsCorrupt_MarksAsUnreadable()
        {
            CreateDummyMod("plugin1.esp", Enumerable.Empty<string>());
            fs.File.WriteAllText(fs.Path.Combine(DataDirectory, "plugin2.esp"), "--- BAD MOD FILE ---");
            CreateDummyMod("plugin3.esp", Enumerable.Empty<string>());
            listings.AddRange(new[]
            {
                CreateDummyListing("plugin1.esp", false),
                CreateDummyListing("plugin2.esp", true),
                CreateDummyListing("plugin3.esp", false),
            });
            setup.Detect(new HashSet<string>());

            Assert.Collection(
                setup.AvailablePlugins,
                x =>
                {
                    Assert.Equal("plugin1.esp", x.FileName);
                    Assert.True(x.IsReadable);
                },
                x =>
                {
                    Assert.Equal("plugin2.esp", x.FileName);
                    Assert.False(x.IsReadable);
                },
                x =>
                {
                    Assert.Equal("plugin3.esp", x.FileName);
                    Assert.True(x.IsReadable);
                });
        }

        private IModListingGetter CreateDummyListing(string pluginName, bool enabled = true)
        {
            var listing = new Mock<IModListingGetter>();
            listing.SetupGet(x => x.Enabled).Returns(enabled);
            listing.SetupGet(x => x.ModKey).Returns(ModKey.FromNameAndExtension(pluginName));
            return listing.Object;
        }

        private IEnumerable<IModListingGetter> CreateDummyListings(params string[] pluginNames)
        {
            return pluginNames.Select(p => CreateDummyListing(p));
        }

        private void CreateDummyMod(string name, IEnumerable<string> masterNames)
        {
            var mod = new SkyrimMod(name, game.GameRelease.ToSkyrimRelease());
            foreach (var masterName in masterNames)
                mod.ModHeader.MasterReferences.Add(new MasterReference { Master = masterName });
            using var stream = fs.File.Create(fs.Path.Combine(DataDirectory, name));
            mod.WriteToBinary(stream, new BinaryWriteParameters
            {
                MastersListContent = MastersListContentOption.NoCheck
            });
        }
    }
}

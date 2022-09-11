using Castle.DynamicProxy;
using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class GroupCacheTests
    {
        private readonly Mock<IReadOnlyGameEnvironment<ISkyrimModGetter>> environmentMock;
        private readonly GroupCache groups;
        private readonly LoadOrder<IModListing<ISkyrimModGetter>> loadOrder;
        private readonly ILogger logger;
        private readonly SkyrimRelease release = SkyrimRelease.SkyrimSE;

        public GroupCacheTests()
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            environmentMock = new Mock<IReadOnlyGameEnvironment<ISkyrimModGetter>>();
            loadOrder = new LoadOrder<IModListing<ISkyrimModGetter>>();
            environmentMock.SetupGet(x => x.LoadOrder).Returns(loadOrder);
            groups = new GroupCache(environmentMock.Object, logger);
        }

        [Fact]
        public void GetAll_ReturnsRecordsFromAllPluginsInPriorityOrder()
        {
            // Real mods shouldn't change editor IDs, but this is just a testing convenience to make sure we're actually
            // getting the specific record data from the specific plugin.
            var mod1 = AddLoadedMod("mod1.esp");
            mod1.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod1Record1" });
            mod1.Shouts.Add(new Shout(FormKey.Factory("000002:mod1.esp"), release) { EditorID = "Mod1Record2" });
            var mod2 = AddLoadedMod("mod2.esp");
            mod2.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod2Record1" });
            mod2.Shouts.Add(new Shout(FormKey.Factory("000002:mod2.esp"), release) { EditorID = "Mod2Record2" });
            var mod3 = AddLoadedMod("mod3.esp");
            mod3.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod3Record1" });
            mod3.Shouts.Add(new Shout(FormKey.Factory("000002:mod1.esp"), release) { EditorID = "Mod3Record2" });
            mod3.Shouts.Add(new Shout(FormKey.Factory("000002:mod2.esp"), release) { EditorID = "Mod3Record3" });
            
            Assert.Collection(
                groups.GetAll(FormKey.Factory("000001:mod1.esp").ToLink<IShoutGetter>()),
                x =>
                {
                    Assert.Equal("mod3.esp", x.Key);
                    Assert.Equal("Mod3Record1", x.Value.EditorID);
                },
                x => {
                    Assert.Equal("mod2.esp", x.Key);
                    Assert.Equal("Mod2Record1", x.Value.EditorID);
                },
                x => {
                    Assert.Equal("mod1.esp", x.Key);
                    Assert.Equal("Mod1Record1", x.Value.EditorID);
                });
            Assert.Collection(
                groups.GetAll(FormKey.Factory("000002:mod1.esp").ToLink<IShoutGetter>()),
                x =>
                {
                    Assert.Equal("mod3.esp", x.Key);
                    Assert.Equal("Mod3Record2", x.Value.EditorID);
                },
                x => {
                    Assert.Equal("mod1.esp", x.Key);
                    Assert.Equal("Mod1Record2", x.Value.EditorID);
                });
            Assert.Collection(
                groups.GetAll(FormKey.Factory("000002:mod2.esp").ToLink<IShoutGetter>()),
                x =>
                {
                    Assert.Equal("mod3.esp", x.Key);
                    Assert.Equal("Mod3Record3", x.Value.EditorID);
                },
                x => {
                    Assert.Equal("mod2.esp", x.Key);
                    Assert.Equal("Mod2Record2", x.Value.EditorID);
                });
        }

        [Fact]
        public void GetBase_WhenPluginNotInLoadOrder_ReturnsNull()
        {
            Assert.Null(groups.Get("mod.esp", RecordType.Shout));
        }

        [Fact]
        public void GetBase_WhenPluginInLoadOrder_ReturnsGroupFromPlugin()
        {
            // For this test, and all of the others, it's far more effective to test with an actual mod using an actual
            // group and records, because the behavior of Mutagen mod objects can be very complex and a mock doesn't
            // accurately capture a lot of it. However, we still need the "mock wrapper" in order to verify that the
            // cache is actually caching, and not looking up the groups/records repeatedly.
            var mod = AddLoadedMod("mod.esp");
            var shouts = mod.Shouts;
            shouts.Add(new Shout(mod, "Shout1"));
            shouts.Add(new Shout(mod, "Shout2"));
            shouts.Add(new Shout(mod, "Shout3"));
            var group = groups.Get("mod.esp", RecordType.Shout);

            Assert.Collection(
                group,
                g => Assert.Equal("Shout1", g.Value.EditorID),
                g => Assert.Equal("Shout2", g.Value.EditorID),
                g => Assert.Equal("Shout3", g.Value.EditorID));
        }

        [Fact]
        public void GetBase_RepeatedRequests_DoNotRepeatLookups()
        {
            var modCalls = new InvocationTracker<ISkyrimMod>();
            var mod = AddLoadedMod("mod.esp", modCalls);
            mod.Shouts.Add(new Shout(mod, "Shout1"));
            var group1 = groups.Get("mod.esp", RecordType.Shout);
            var group2 = groups.Get("mod.esp", RecordType.Shout);
            var group3 = groups.Get("mod.esp", RecordType.Shout);

            Assert.Equal(group2.Keys, group1.Keys);
            Assert.Equal(group3.Keys, group1.Keys);
            Assert.Single(modCalls.Property(x => x.Shouts));
        }

        [Fact]
        public void GetGeneric_WhenPluginNotInLoadOrder_ReturnsNull()
        {
            Assert.Null(groups.Get("mod.esp", x => x.Shouts));
        }

        [Fact]
        public void GetGeneric_WhenPluginInLoadOrder_ReturnsGroupFromPlugin()
        {
            var mod = AddLoadedMod("mod.esp");
            var shouts = mod.Shouts;
            shouts.Add(new Shout(mod, "Shout1"));
            shouts.Add(new Shout(mod, "Shout2"));
            shouts.Add(new Shout(mod, "Shout3"));
            var group = groups.Get("mod.esp", x => x.Shouts);

            Assert.Collection(
                group,
                g => Assert.Equal("Shout1", g.EditorID),
                g => Assert.Equal("Shout2", g.EditorID),
                g => Assert.Equal("Shout3", g.EditorID));
        }

        [Fact]
        public void GetGeneric_RepeatedRequests_DoNotRepeatLookups()
        {
            var modCalls = new InvocationTracker<ISkyrimMod>();
            var mod = AddLoadedMod("mod.esp", modCalls);
            mod.Shouts.Add(new Shout(mod, "Shout1"));
            var group1 = groups.Get("mod.esp", x => x.Shouts);
            var group2 = groups.Get("mod.esp", x => x.Shouts);
            var group3 = groups.Get("mod.esp", x => x.Shouts);

            Assert.Equal(group2.ToList(), group1.ToList());
            Assert.Equal(group3.ToList(), group1.ToList());
            Assert.Single(modCalls.Property(x => x.Shouts));
        }

        [Fact]
        public void GetMod_WhenNotLoaded_ReturnsNull()
        {
            Assert.Null(groups.GetMod("mod.esp"));
        }

        [Fact]
        public void GetMod_WhenLoaded_ReturnsMod()
        {
            var mod = AddLoadedMod("mod.esp");

            Assert.Same(mod, groups.GetMod("mod.esp"));
        }

        [Fact]
        public void GetWinner_ReturnsWinningOverride()
        {
            // Real mods shouldn't change editor IDs, but this is just a testing convenience to make sure we're actually
            // getting the specific record data from the specific plugin.
            var mod1 = AddLoadedMod("mod1.esp");
            mod1.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod1Record" });
            var mod2 = AddLoadedMod("mod2.esp");
            mod2.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod2Record" });
            mod2.Shouts.Add(new Shout(FormKey.Factory("000002:mod2.esp"), release) { EditorID = "DummyRecord" });
            var mod3 = AddLoadedMod("mod3.esp");
            mod3.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod3Record" });

            var winner = groups.GetWinner(FormKey.Factory("000001:mod1.esp").ToLink<Shout>());
            Assert.Equal("Mod3Record", winner.EditorID);
        }

        [Fact]
        public void GetWinnerWithSource_ReturnsWinningOverrideWithPlugin()
        {
            var mod1 = AddLoadedMod("mod1.esp");
            mod1.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod1Record" });
            var mod2 = AddLoadedMod("mod2.esp");
            mod2.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod2Record" });
            mod2.Shouts.Add(new Shout(FormKey.Factory("000002:mod2.esp"), release) { EditorID = "DummyRecord" });
            var mod3 = AddLoadedMod("mod3.esp");
            mod3.Shouts.Add(new Shout(FormKey.Factory("000001:mod1.esp"), release) { EditorID = "Mod3Record" });

            var winner = groups.GetWinnerWithSource(FormKey.Factory("000001:mod1.esp").ToLink<Shout>());
            Assert.Equal("mod3.esp", winner.Key);
            Assert.Equal("Mod3Record", winner.Value.EditorID);
        }

        [Fact]
        public void MasterExists_WhenModNotLoaded_ReturnsFalse()
        {
            Assert.False(groups.MasterExists(FormKey.Factory("000002:mod.esp"), RecordType.Shout));
        }

        [Fact]
        public void MasterExists_WhenRecordMissingInMaster_ReturnsFalse()
        {
            var mod = AddLoadedMod("mod.esp");
            mod.Shouts.Add(new Shout(FormKey.Factory("000001:mod.esp"), release));

            Assert.False(groups.MasterExists(FormKey.Factory("000002:mod.esp"), RecordType.Shout));
        }

        [Fact]
        public void MasterExists_WhenRecordWrongTypeInMaster_ReturnsFalse()
        {
            var mod = AddLoadedMod("mod.esp");
            mod.Outfits.Add(new Outfit(FormKey.Factory("000002:mod.esp"), release));

            Assert.False(groups.MasterExists(FormKey.Factory("000002:mod.esp"), RecordType.Shout));
        }

        [Fact]
        public void MasterExists_WhenRecordPresentInMaster_ReturnsTrue()
        {
            var mod = AddLoadedMod("mod.esp");
            mod.Shouts.Add(new Shout(FormKey.Factory("000002:mod.esp"), release));

            Assert.True(groups.MasterExists(FormKey.Factory("000002:mod.esp"), RecordType.Shout));
        }

        private ISkyrimMod AddLoadedMod(string fileName, InvocationTracker<ISkyrimMod> tracker = null)
        {
            var mod = new SkyrimMod(fileName, release);
            var interceptors = new List<IInterceptor>();
            if (tracker != null)
                interceptors.Add(tracker);
            var proxiedMod = new ProxyGenerator().CreateInterfaceProxyWithTarget<ISkyrimMod>(mod, interceptors.ToArray());
            loadOrder.Add(new ModListing<ISkyrimModGetter>(proxiedMod));
            return proxiedMod;
        }
    }
}

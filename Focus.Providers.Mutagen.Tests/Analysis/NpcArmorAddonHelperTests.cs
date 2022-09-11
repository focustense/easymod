using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class NpcArmorAddonHelperTests
    {
        private readonly FakeGroupCache groups;
        private readonly NpcAnalyzer.ArmorAddonHelper helper;

        public NpcArmorAddonHelperTests()
        {
            groups = new FakeGroupCache();
            helper = new NpcAnalyzer.ArmorAddonHelper(groups);
        }

        [Fact]
        public void IsDefaultSkin_TrueForDefaultRaceSkinAddon()
        {
            var addon = new ArmorAddon(FormKey.Factory("123456:plugin.esp"), SkyrimRelease.SkyrimSE);
            var checkedAddonKey = groups.AddRecords<ArmorAddon>("plugin.esp", _ => { })[0].ToFormKey();
            var raceKey = groups.AddRecords<Race>("plugin.esp", r =>
            {
                var defaultSkinKey = groups.AddRecords<Armor>("plugin.esp", a =>
                {
                    // These don't really have to exist since we aren't doing deep inspection of the addons here.
                    a.Armature.Add(FormKey.Factory("111111:plugin.esp"));
                    a.Armature.Add(FormKey.Factory("222222:plugin.esp"));
                    a.Armature.Add(FormKey.Factory("333333:plugin.esp"));
                    a.Armature.Add(addon.FormKey);
                })[0].ToFormKey();
                r.Skin.SetTo(defaultSkinKey);
            })[0].ToFormKey();

            Assert.True(helper.IsDefaultSkin(addon, raceKey.ToLinkGetter<IRaceGetter>()));
        }

        [Fact]
        public void IsDefaultSkin_FalseForNonDefaultRaceSkinAddon()
        {
            var addon = new ArmorAddon(FormKey.Factory("123456:plugin.esp"), SkyrimRelease.SkyrimSE);
            var checkedAddonKey = groups.AddRecords<ArmorAddon>("plugin.esp", _ => { })[0].ToFormKey();
            var raceKey = groups.AddRecords<Race>("plugin.esp", r =>
            {
                var defaultSkinKey = groups.AddRecords<Armor>("plugin.esp", a =>
                {
                    a.Armature.Add(FormKey.Factory("111111:plugin.esp"));
                    a.Armature.Add(FormKey.Factory("222222:plugin.esp"));
                    a.Armature.Add(FormKey.Factory("333333:plugin.esp"));
                })[0].ToFormKey();
                r.Skin.SetTo(defaultSkinKey);
            })[0].ToFormKey();

            Assert.False(helper.IsDefaultSkin(addon, raceKey.ToLinkGetter<IRaceGetter>()));
        }

        [Theory]
        [InlineData(BipedObjectFlag.Hair)]
        [InlineData(BipedObjectFlag.LongHair)]
        [InlineData(BipedObjectFlag.Hair | BipedObjectFlag.LongHair)]
        public void ReplacesHair_TrueForOnlyHairFlags(BipedObjectFlag flags)
        {
            var addon = new ArmorAddon(FormKey.Null, SkyrimRelease.SkyrimSE);
            addon.BodyTemplate = new() { FirstPersonFlags = flags };

            Assert.True(helper.ReplacesHair(addon));
        }

        [Theory]
        [InlineData((BipedObjectFlag)0)]
        [InlineData(BipedObjectFlag.Body)]
        [InlineData(BipedObjectFlag.Circlet)]
        [InlineData(BipedObjectFlag.Body | BipedObjectFlag.Hair)]
        [InlineData(BipedObjectFlag.Feet | BipedObjectFlag.Forearms | BipedObjectFlag.LongHair)]
        public void ReplacesHair_FalseForAnyNonHairFlags(BipedObjectFlag flags)
        {
            var addon = new ArmorAddon(FormKey.Null, SkyrimRelease.SkyrimSE);
            addon.BodyTemplate = new() { FirstPersonFlags = flags };

            Assert.False(helper.ReplacesHair(addon));
        }

        [Fact]
        public void SupportsRace_TrueForPrimaryRaceMatch()
        {
            var raceKey = groups.AddRecords<Race>("plugin.esp", x => x.EditorID = "Race1")[0].ToFormKey();
            var addon = new ArmorAddon(FormKey.Null, SkyrimRelease.SkyrimSE);
            addon.Race.SetTo(raceKey);

            Assert.True(helper.SupportsRace(addon, raceKey.ToLinkGetter<IRaceGetter>()));
        }

        [Fact]
        public void SupportsRace_TrueForAdditionalRaceMatch()
        {
            var raceKeys = groups.AddRecords<Race>(
                "plugin.esp", x => x.EditorID = "Race1", x => x.EditorID = "Race2", x => x.EditorID = "Race3")
                .ToFormKeys().ToList();
            var addon = new ArmorAddon(FormKey.Null, SkyrimRelease.SkyrimSE);
            addon.Race.SetTo(raceKeys[0]);
            addon.AdditionalRaces.Add(raceKeys[2].ToLinkGetter<IRaceGetter>());

            Assert.True(helper.SupportsRace(addon, raceKeys[2].ToLinkGetter<IRaceGetter>()));
        }

        [Fact]
        public void SupportsRace_FalseForNoRaceMatch()
        {
            var raceKeys = groups.AddRecords<Race>(
                "plugin.esp", x => x.EditorID = "Race1", x => x.EditorID = "Race2", x => x.EditorID = "Race3")
                .ToFormKeys().ToList();
            var addon = new ArmorAddon(FormKey.Null, SkyrimRelease.SkyrimSE);
            addon.Race.SetTo(raceKeys[0]);
            addon.AdditionalRaces.Add(raceKeys[2].ToLinkGetter<IRaceGetter>());

            Assert.False(helper.SupportsRace(addon, raceKeys[1].ToLinkGetter<IRaceGetter>()));
        }
    }
}

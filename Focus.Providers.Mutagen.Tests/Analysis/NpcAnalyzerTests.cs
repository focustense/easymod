using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class NpcAnalyzerTests : CommonAnalyzerFacts<NpcAnalyzer, Npc, NpcAnalysis>
    {
        private readonly Mock<NpcAnalyzer.IArmorAddonHelper> armorAddonHelperMock = new();

        private NpcComparisonDependencies comparisonDependencies;

        public NpcAnalyzerTests()
        {
            Analyzer = new NpcAnalyzer(Groups, armorAddonHelperMock.Object, ReferenceChecker.Of<INpcGetter>());
        }

        [Fact]
        public void IncludesName()
        {
            var npcKeys = Groups.AddRecords<Npc>("mod.esp", npc => npc.Name = "Bob");
            var analysis = Analyzer.Analyze("mod.esp", npcKeys[0]);

            Assert.Equal("Bob", analysis.Name);
        }

        [Fact]
        public void MergesRaceWithFemaleNpcHeadParts()
        {
            var vanillaHeadPartKeys = Groups.AddRecords<HeadPart>(
                "vanilla.esp",
                x => { x.EditorID = "VanillaFace"; x.Type = HeadPart.TypeEnum.Face; },
                x => { x.EditorID = "VanillaEyes"; x.Type = HeadPart.TypeEnum.Eyes; },
                x => { x.EditorID = "VanillaHair"; x.Type = HeadPart.TypeEnum.Hair; },
                x => { x.EditorID = "VanillaFacialHair"; x.Type = HeadPart.TypeEnum.FacialHair; });
            var raceKeys = Groups.AddRecords<Race>(
                "vanilla.esp", r => r.HeadData = CreateHeadData(vanillaHeadPartKeys.ToFormKeys(), true));
            var moddedHeadPartKeys = Groups.AddRecords<HeadPart>(
                "mod.esp",
                x => { x.EditorID = "ModdedFace"; x.Type = HeadPart.TypeEnum.Face; },
                x => { x.EditorID = "ModdedHair"; x.Type = HeadPart.TypeEnum.Hair; });
            var npcKeys = Groups.AddRecords<Npc>("mod.esp", npc =>
            {
                npc.Configuration.Flags |= NpcConfiguration.Flag.Female;
                npc.Race.SetTo(raceKeys[0].ToFormKey());
                npc.HeadParts.AddRange(moddedHeadPartKeys.ToFormKeys());
            });
            var analysis = Analyzer.Analyze("mod.esp", npcKeys[0]);

            Assert.Equal(
                new[]
                {
                    moddedHeadPartKeys[0], // Face
                    moddedHeadPartKeys[1], // Hair
                    vanillaHeadPartKeys[1], // Eyes
                    vanillaHeadPartKeys[3], // Facial hair
                },
                // Hack until we get Assert.Equivalent
                analysis.MainHeadParts.OrderBy(x => x.BasePluginName).ThenBy(x => x.LocalFormIdHex));
        }

        [Fact]
        public void MergesRaceWithMaleNpcHeadParts()
        {
            var vanillaHeadPartKeys = Groups.AddRecords<HeadPart>(
                "vanilla.esp",
                x => { x.EditorID = "VanillaFace"; x.Type = HeadPart.TypeEnum.Face; },
                x => { x.EditorID = "VanillaEyes"; x.Type = HeadPart.TypeEnum.Eyes; },
                x => { x.EditorID = "VanillaHair"; x.Type = HeadPart.TypeEnum.Hair; },
                x => { x.EditorID = "VanillaFacialHair"; x.Type = HeadPart.TypeEnum.FacialHair; });
            var raceKeys = Groups.AddRecords<Race>(
                "vanilla.esp", r => r.HeadData = CreateHeadData(vanillaHeadPartKeys.ToFormKeys(), false));
            var moddedHeadPartKeys = Groups.AddRecords<HeadPart>(
                "mod.esp",
                x => { x.EditorID = "ModdedFace"; x.Type = HeadPart.TypeEnum.Face; },
                x => { x.EditorID = "ModdedHair"; x.Type = HeadPart.TypeEnum.Hair; },
                x => { x.EditorID = "Scar1"; x.Type = HeadPart.TypeEnum.Scars; },
                x => { x.EditorID = "Scar2"; x.Type = HeadPart.TypeEnum.Scars; });
            var npcKeys = Groups.AddRecords<Npc>("mod.esp", npc =>
            {
                npc.Race.SetTo(raceKeys[0].ToFormKey());
                npc.HeadParts.AddRange(moddedHeadPartKeys.ToFormKeys());
            });
            var analysis = Analyzer.Analyze("mod.esp", npcKeys[0]);

            Assert.Equal(
                new[]
                {
                    moddedHeadPartKeys[0], // Face
                    moddedHeadPartKeys[1], // Hair
                    moddedHeadPartKeys[2], // Scar 1
                    moddedHeadPartKeys[3], // Scar 2
                    vanillaHeadPartKeys[1], // Eyes
                    vanillaHeadPartKeys[3], // Facial hair
                },
                // Hack until we get Assert.Equivalent
                analysis.MainHeadParts.OrderBy(x => x.BasePluginName).ThenBy(x => x.LocalFormIdHex));
        }

        [Fact]
        public void WhenConfigExcludesFemaleFlag_IsFemale_IsTrue()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.IsFemale);
        }

        [Fact]
        public void WhenConfigIncludesFemaleFlag_IsFemale_IsTrue()
        {
            var npcKeys = Groups.AddRecords<Npc>(
                "plugin.esp", x => x.Configuration.Flags |= NpcConfiguration.Flag.Female);
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.IsFemale);
        }

        [Fact]
        public void WhenConfigExcludesUseTemplateFlag_IsAudioTemplate_IsTrue()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.IsAudioTemplate);
        }

        [Fact]
        public void WhenConfigIncludesUseTemplateFlag_IsAudioTemplate_IsTrue()
        {
            var npcKeys = Groups.AddRecords<Npc>(
                "plugin.esp", x => x.Configuration.Flags |= NpcConfiguration.Flag.UseTemplate);
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.IsAudioTemplate);
        }

        [Fact]
        public void WhenRaceInvalid_CanUseFaceGen_IsFalse()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(FormKey.Factory("123456:plugin.esp")));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.CanUseFaceGen);
        }

        [Fact]
        public void WhenRaceExcludesFaceGenFlag_CanUseFaceGen_IsFalse()
        {
            var raceKeys = Groups.AddRecords<Race>("plugin.esp", x => { });
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(raceKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.CanUseFaceGen);
        }

        [Fact]
        public void WhenRaceIncludesFaceGenFlag_CanUseFaceGen_IsFalse()
        {
            var raceKeys = Groups.AddRecords<Race>("plugin.esp", x => x.Flags |= Race.Flag.FaceGenHead);
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(raceKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.CanUseFaceGen);
        }

        [Fact]
        public void WhenRaceInvalid_IsChild_IsFalse()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(FormKey.Factory("123456:plugin.esp")));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.IsChild);
        }

        [Fact]
        public void WhenRaceExcludesChildFlag_IsChild_IsFalse()
        {
            var raceKeys = Groups.AddRecords<Race>("plugin.esp", x => { });
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(raceKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.IsChild);
        }

        [Fact]
        public void WhenRaceIncludesChildFlag_IsChild_IsFalse()
        {
            var raceKeys = Groups.AddRecords<Race>("plugin.esp", x => x.Flags |= Race.Flag.Child);
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.Race.SetTo(raceKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.IsChild);
        }

        [Fact]
        public void WhenWornArmorNotSet_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorHasNoHairAddons_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorHasUnsupportedHairAddon_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHair", ReplacesHair = true, SupportsRace = false, IsDefaultSkin = false },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorHasDefaultHairAddon_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHair", ReplacesHair = true, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorHasSingleSupportedNonDefaultHairAddon_WigInfo_DescribesMatchingAddon()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHair", ReplacesHair = true, SupportsRace = true, IsDefaultSkin = false },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "UnusedHair", ReplacesHair = true, SupportsRace = false, IsDefaultSkin = false },
                new() { EditorId = "UnusedFeet", ReplacesHair = false, SupportsRace = false, IsDefaultSkin = false }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.NotNull(analysis.WigInfo);
            Assert.Equal("TestHair", analysis.WigInfo.EditorId);
        }

        [Fact]
        public void WhenWornArmorHasMultipleSupportedNonDefaultHairAddons_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestHair", ReplacesHair = true, SupportsRace = true, IsDefaultSkin = false },
                new() { EditorId = "TestHair2", ReplacesHair = true, SupportsRace = true, IsDefaultSkin = false },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorIncludesSupportedNonDefaultNonHairAddons_WigInfo_IsNull()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => SetupWigScenario(x,
                new() { EditorId = "TestBody", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = false },
                new() { EditorId = "TestHair", ReplacesHair = true, SupportsRace = true, IsDefaultSkin = false },
                new() { EditorId = "TestHands", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true },
                new() { EditorId = "TestFeet", ReplacesHair = false, SupportsRace = true, IsDefaultSkin = true }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenNpcHasWig_AndNoHairInHeadParts_WigInfo_IsBald()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>(
                "plugin.esp",
                x => { x.Type = HeadPart.TypeEnum.Face; x.Model = new() { File = "face.nif" }; },
                x => { x.Type = HeadPart.TypeEnum.Eyes; x.Model = new() { File = "eyes.nif" }; });
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.HeadParts.AddRange(headPartKeys.ToFormKeys());
                SetupPositiveWigScenario(x, "DummyHair");
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.WigInfo.IsBald);
        }

        [Fact]
        public void WhenNpcHasWig_AndWinningHairHasNoModel_WigInfo_IsBald()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>(
                "plugin.esp",
                x => { x.Type = HeadPart.TypeEnum.Hair; x.Model = new() { File = "hair.nif" }; },
                x => { x.Type = HeadPart.TypeEnum.Hair; x.Model = new(); });
            var raceKeys = Groups.AddRecords<Race>(
                "plugin.esp", x => x.HeadData = CreateHeadData(headPartKeys.Take(1).ToFormKeys(), false));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.Race.SetTo(raceKeys[0].ToFormKey());
                x.HeadParts.Add(headPartKeys[1].ToFormKey());
                SetupPositiveWigScenario(x, raceKeys[0].ToFormKey(), "DummyHair");
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.WigInfo.IsBald);
        }

        [Fact]
        public void WhenNpcHasWig_AndWinningHairHasModel_WigInfo_IsNotBald()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>(
                "plugin.esp",
                x => { x.Type = HeadPart.TypeEnum.Hair; x.Model = new() { File = "hair.nif" }; });
            var raceKeys = Groups.AddRecords<Race>(
                "plugin.esp", x => x.HeadData = CreateHeadData(headPartKeys.ToFormKeys(), false));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.Race.SetTo(raceKeys[0].ToFormKey());
                SetupPositiveWigScenario(x, raceKeys[0].ToFormKey(), "DummyHair");
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.WigInfo.IsBald);
        }

        [Fact]
        public void WhenNpcHasWig_AndWigHasWorldModel_WigInfo_IncludesGenderedModelName()
        {
            Npc maleNpc = null;
            var npcKeys = Groups.AddRecords<Npc>(
                "plugin.esp",
                npc =>
                {
                    SetupPositiveWigScenario(npc, "Dummy", x =>
                    {
                        x.WorldModel = new GenderedItem<Model>(
                            new() { File = @"foo/bar/baz/wigmodelmale.nif" },
                            new() { File = @"foo/bar/baz/wigmodelfemale.nif" });
                    });
                    maleNpc = npc;
                },
                x =>
                {
                    x.DeepCopyIn(maleNpc);
                    x.Configuration.Flags |= NpcConfiguration.Flag.Female;
                });
            var maleAnalysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);
            var femaleAnalysis = Analyzer.Analyze("plugin.esp", npcKeys[1]);

            Assert.Equal("wigmodelmale", maleAnalysis.WigInfo.ModelName);
            Assert.Equal("wigmodelfemale", femaleAnalysis.WigInfo.ModelName);
        }

        [Fact]
        public void WhenIdenticalToMaster_AllComparisons_AreEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", SetupNpcForComparison);
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.True(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Behavior), MemberType = typeof(NpcMutations))]
        public void WhenBehaviorModified_BehaviorComparisons_AreNotEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.True(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Face), MemberType = typeof(NpcMutations))]
        public void WhenFaceModified_FaceComparisons_AreNotEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.True(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHairOverridenWithSameReference_HairComparisons_AreEqual()
        {
            // What we're interested in here is if an NPC overrides the hair, but using a head part that is effectively
            // the same as what that NPC would have had anyway, i.e. from the race.
            // Since our fixture NPC already has custom hair, it needs to be removed from the controls.
            void revertHair(Npc npc) =>
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair);
            var npcKeys = Groups.AddRecords<Npc>("master.esp", x => SetupNpcForComparison(x, revertHair));
            Groups.AddRecords<Npc>("override.esp", "master.esp", x => SetupNpcForComparison(x, revertHair));
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                revertHair(npc);
                var race = npc.Race.WinnerFrom(Groups);
                npc.HeadParts.Add(GetDefaultHeadPartKey(race, true, HeadPart.TypeEnum.Hair));
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHairOverridenWithDifferentReference_HairComparisons_AreNotEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var hairKeys = Groups.AddRecords<HeadPart>("plugin.esp", x =>
                {
                    x.EditorID = "HairFemaleNord2";
                    x.Type = HeadPart.TypeEnum.Hair;
                });
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair);
                npc.HeadParts.Add(hairKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.True(comparison.ModifiesFace);
                Assert.True(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHairRevertedToRaceDefault_HairComparisons_AreNotEqual()
        {
            // If the master NPC does define a non-default hair, then changing it back to the default for the race IS an
            // important edit and requires new facegen. Although this likely doesn't test any code that the "different
            // reference" case doesn't already cover, it's worth having because there is special logic to deal with the
            // NPC-race inheritance.
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair);
                npc.HeadParts.Add(GetDefaultHeadPartKey(npc.Race.WinnerFrom(Groups), true, HeadPart.TypeEnum.Hair));
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.True(comparison.ModifiesFace);
                Assert.True(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHeadPartOverridenWithSameReference_HeadPartComparisons_AreEqual()
        {
            void revertEyes(Npc npc) =>
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Eyes);
            var npcKeys = Groups.AddRecords<Npc>("master.esp", x => SetupNpcForComparison(x, revertEyes));
            Groups.AddRecords<Npc>("override.esp", "master.esp", x => SetupNpcForComparison(x, revertEyes));
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                revertEyes(npc);
                var race = npc.Race.WinnerFrom(Groups);
                npc.HeadParts.Add(GetDefaultHeadPartKey(race, true, HeadPart.TypeEnum.Eyes));
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHeadPartOverridenWithDifferentReference_HeadPartComparisons_AreNotEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var headPartKeys = Groups.AddRecords<HeadPart>("plugin.esp", x =>
                {
                    x.EditorID = "FemaleEyesHumanDemon";
                    x.Type = HeadPart.TypeEnum.Eyes;
                });
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Eyes);
                npc.HeadParts.Add(headPartKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.True(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenHeadPartRevertedToRaceDefault_HeadPartComparisons_AreNotEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Eyes);
                npc.HeadParts.Add(GetDefaultHeadPartKey(npc.Race.WinnerFrom(Groups), true, HeadPart.TypeEnum.Eyes));
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.True(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Outfits), MemberType = typeof(NpcMutations))]
        public void WhenOutfitsModified_OutfitComparisons_AreNotEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.True(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenRaceChangeAffectsHair_HairComparisons_AreNotEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                // Race edit only matters if the NPC doesn't already override the head parts that would be changed.
                // For this test, we need to remove the NPC's existing custom hair.
                npc.HeadParts.RemoveAll(x => x.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair);

                var oldRace = npc.Race.WinnerFrom(Groups);
                var headPartKeys = Groups.AddRecords<HeadPart>("master.esp", x =>
                {
                    x.EditorID = "HairFemaleRacy1";
                    x.Type = HeadPart.TypeEnum.Hair;
                });
                var raceKeys = Groups.AddRecords<Race>("racy.esp", r =>
                {
                    r.EditorID = "RacyRace";
                    r.HeadData = new GenderedItem<HeadData>(
                        oldRace.HeadData.Male.DeepCopy(), oldRace.HeadData.Female.DeepCopy());
                    r.HeadData.Female.HeadParts
                        .Single(x => x.Head.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair)
                        .Head.SetTo(headPartKeys[0].ToFormKey());
                });
                npc.Race.SetTo(raceKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                // Skip the behavior assertion, whether or not a race edit qualifies is not important to this test.
                Assert.True(comparison.ModifiesFace);
                Assert.True(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.True(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenRaceChangeDoesNotAffectHair_HairComparisons_AreEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                // NOT removing the hair here means that the NPC still overrides the race default, and therefore her
                // hair has not actually been affected by the race change.

                var oldRace = npc.Race.WinnerFrom(Groups);
                var headPartKeys = Groups.AddRecords<HeadPart>("master.esp", x =>
                {
                    x.EditorID = "HairFemaleRacy1";
                    x.Type = HeadPart.TypeEnum.Hair;
                });
                var raceKeys = Groups.AddRecords<Race>("racy.esp", r =>
                {
                    r.EditorID = "RacyRace";
                    r.HeadData = new GenderedItem<HeadData>(
                        oldRace.HeadData.Male.DeepCopy(), oldRace.HeadData.Female.DeepCopy());
                    r.HeadData.Female.HeadParts
                        .Single(x => x.Head.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Hair)
                        .Head.SetTo(headPartKeys[0].ToFormKey());
                });
                npc.Race.SetTo(raceKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                // Skip the behavior assertion, whether or not a race edit qualifies is not important to this test.
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.True(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenRaceChangeAffectsHeadParts_HeadPartComparisons_AreNotEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var oldRace = npc.Race.WinnerFrom(Groups);
                var headPartKeys = Groups.AddRecords<HeadPart>("master.esp", x =>
                {
                    x.EditorID = "FemaleHeadRacy";
                    x.Type = HeadPart.TypeEnum.Face;
                });
                var raceKeys = Groups.AddRecords<Race>("racy.esp", r =>
                {
                    r.EditorID = "RacyRace";
                    r.HeadData = new GenderedItem<HeadData>(
                        oldRace.HeadData.Male.DeepCopy(), oldRace.HeadData.Female.DeepCopy());
                    r.HeadData.Female.HeadParts
                        .Single(x => x.Head.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Face)
                        .Head.SetTo(headPartKeys[0].ToFormKey());
                });
                npc.Race.SetTo(raceKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                // Skip the behavior assertion, whether or not a race edit qualifies is not important to this test.
                Assert.True(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.True(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.True(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Fact]
        public void WhenRaceChangeDoesNotAffectHeadParts_HeadPartComparisons_AreEqual()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var oldRace = npc.Race.WinnerFrom(Groups);
                // We should be able to replace ALL of the default race head parts that have types overridden by the
                // NPC, and still not have it count as a "head part change".
                // Hair has already been tested separately, no need to re-test.
                var headPartKeys = Groups.AddRecords<HeadPart>(
                    "master.esp",
                    x =>
                    {
                        x.EditorID = "BrowsFemaleRacy1";
                        x.Type = HeadPart.TypeEnum.Eyebrows;
                    },
                    x =>
                    {
                        x.EditorID = "EyesFemaleRacy1";
                        x.Type = HeadPart.TypeEnum.Eyes;
                    });
                var raceKeys = Groups.AddRecords<Race>("racy.esp", r =>
                {
                    r.EditorID = "RacyRace";
                    r.HeadData = new GenderedItem<HeadData>(
                        oldRace.HeadData.Male.DeepCopy(), oldRace.HeadData.Female.DeepCopy());
                    r.HeadData.Female.HeadParts
                        .Single(x => x.Head.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Eyebrows)
                        .Head.SetTo(headPartKeys[0].ToFormKey());
                    r.HeadData.Female.HeadParts
                        .Single(x => x.Head.WinnerFrom(Groups).Type == HeadPart.TypeEnum.Eyes)
                        .Head.SetTo(headPartKeys[1].ToFormKey());
                });
                npc.Race.SetTo(raceKeys[0].ToFormKey());
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                // Skip the behavior assertion, whether or not a race edit qualifies is not important to this test.
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.True(comparison.ModifiesRace);
                Assert.False(comparison.ModifiesScales);
            });
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Scales), MemberType = typeof(NpcMutations))]
        public void WhenScalesModified_ScaleComparisons_AreNotEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
                Assert.True(comparison.ModifiesScales);
            });
        }

        public enum SkinTestChange { OriginalNpcSkin, ModdedNpcRace, ModdedNpcRaceSkin, ModdedNpcSkin };

        [Theory]
        [InlineData(false)]
        [InlineData(false, SkinTestChange.OriginalNpcSkin, SkinTestChange.ModdedNpcSkin)]
        [InlineData(false, SkinTestChange.ModdedNpcRace /* race with default skin */)]
        [InlineData(false, SkinTestChange.OriginalNpcSkin, SkinTestChange.ModdedNpcRace, SkinTestChange.ModdedNpcRaceSkin)]
        [InlineData(false, SkinTestChange.OriginalNpcSkin, SkinTestChange.ModdedNpcRace, SkinTestChange.ModdedNpcSkin)]
        [InlineData(true, SkinTestChange.OriginalNpcSkin)]
        [InlineData(true, SkinTestChange.ModdedNpcSkin)]
        [InlineData(true, SkinTestChange.ModdedNpcRace, SkinTestChange.ModdedNpcSkin)]
        [InlineData(true, SkinTestChange.ModdedNpcRace, SkinTestChange.ModdedNpcRaceSkin)]
        public void WhenEffectiveSkinsSame_SkinComparisons_AreEqual(
            bool expectedResult, params SkinTestChange[] changes)
        {
            var customSkinKey = AddEmptyRecords<Armor>("dummy.esp", "NewSkin").Single();
            var npcKeys = Groups.AddRecords<Npc>(
                "master.esp", x => SetupNpcForComparison(x, npc =>
                {
                    if (changes.Contains(SkinTestChange.OriginalNpcSkin))
                        npc.WornArmor.SetTo(customSkinKey);
                }));
            Groups.AddRecords<Npc>(
                "override.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
                {
                    if (changes.Contains(SkinTestChange.OriginalNpcSkin))
                        npc.WornArmor.SetTo(customSkinKey);
                }));
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                if (changes.Contains(SkinTestChange.ModdedNpcRace))
                {
                    var newRaceKeys = Groups.AddRecords<Race>("plugin.esp", r =>
                        r.Skin.SetTo(changes.Contains(
                            SkinTestChange.ModdedNpcRaceSkin) ? customSkinKey : comparisonDependencies.RaceSkinKey));
                    npc.Race.SetTo(newRaceKeys[0].ToFormKey());
                }
                if (changes.Contains(SkinTestChange.ModdedNpcSkin))
                    npc.WornArmor.SetTo(customSkinKey);
            }));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.Equal(expectedResult, comparison.ModifiesSkin);
            });
        }

        [Fact]
        public void WhenCustomSkinNotSpecified_ProvidesDefaultRaceSkin()
        {
            var skinKey = FormKey.Factory("123456:plugin.esp");
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                var raceKeys = Groups.AddRecords<Race>(
                    "plugin.esp", race => race.Skin.SetTo(skinKey));
                npc.Race.SetTo(raceKeys[0].ToFormKey());
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(skinKey.ToRecordKey(), analysis.SkinKey);
        }

        [Fact]
        public void WhenCustomSkinSpecified_OverridesDefaultRaceSkin()
        {
            var raceSkinKey = FormKey.Factory("123456:plugin.esp");
            var npcSkinKey = FormKey.Factory("223344:plugin.esp");
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                var raceKeys = Groups.AddRecords<Race>(
                    "plugin.esp", race => race.Skin.SetTo(raceSkinKey));
                npc.Race.SetTo(raceKeys[0].ToFormKey());
                npc.WornArmor.SetTo(npcSkinKey);
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(npcSkinKey.ToRecordKey(), analysis.SkinKey);
        }

        [Fact]
        public void WhenTemplateNotUsed_TemplateInfoIsNull()
        {
            var npcKeys = AddEmptyRecords<Npc>("plugin.esp", "DummyNpc");
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0].ToRecordKey());

            Assert.Null(analysis.TemplateInfo);
        }

        [Fact]
        public void WhenTemplateIsMissing_TemplateInfoIsInvalidType()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                npc.Template.SetTo(FormKey.Factory("123456:plugin.esp"));
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal("123456:plugin.esp", analysis.TemplateInfo.Key.ToString());
            Assert.Equal(NpcTemplateTargetType.Invalid, analysis.TemplateInfo.TargetType);
        }

        [Fact]
        public void WhenTemplateIsLeveledNpc_TemplateInfoIsLeveledNpcType()
        {
            var leveledNpcKeys = Groups.AddRecords<LeveledNpc>("plugin.esp", _ => { });
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                npc.Template.SetTo(leveledNpcKeys[0].ToFormKey());
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(leveledNpcKeys[0], analysis.TemplateInfo.Key);
            Assert.Equal(NpcTemplateTargetType.LeveledNpc, analysis.TemplateInfo.TargetType);
        }

        [Fact]
        public void WhenTemplateIsNpc_TemplateInfoIsNpcType()
        {
            var targetNpcKeys = Groups.AddRecords<Npc>("plugin.esp", _ => { });
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                npc.Template.SetTo(targetNpcKeys[0].ToFormKey());
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(targetNpcKeys[0], analysis.TemplateInfo.Key);
            Assert.Equal(NpcTemplateTargetType.Npc, analysis.TemplateInfo.TargetType);
        }

        [Fact]
        public void WhenTemplateExcludesTraitFlag_TemplateInfoDoesNotInheritTraits()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                npc.Template.SetTo(FormKey.Factory("123456:plugin.esp"));
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.TemplateInfo.InheritsTraits);
        }

        [Fact]
        public void WhenTemplateIncludesTraitFlag_TemplateInfoInheritsTraits()
        {
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", npc =>
            {
                npc.Configuration.TemplateFlags |= NpcConfiguration.TemplateFlag.Traits;
                npc.Template.SetTo(FormKey.Factory("123456:plugin.esp"));
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.True(analysis.TemplateInfo.InheritsTraits);
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Ignored), MemberType = typeof(NpcMutations))]
        public void WhenIgnoredAttributesModified_AllComparisonsExceptIdentical_AreEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.False(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
            });
        }

        [Theory]
        [MemberData(nameof(NpcMutations.Unused), MemberType = typeof(NpcMutations))]
        public void WhenUnusedAttributesModified_AllComparisons_AreEqual(Action<Npc> mutation)
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", x => SetupNpcForComparison(x, mutation));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            AssertComparisons(analysis, "master.esp", "override.esp", comparison =>
            {
                Assert.True(comparison.IsIdentical);
                Assert.False(comparison.ModifiesBehavior);
                Assert.False(comparison.ModifiesSkin);
                Assert.False(comparison.ModifiesFace);
                Assert.False(comparison.ModifiesHair);
                Assert.False(comparison.ModifiesHeadParts);
                Assert.False(comparison.ModifiesOutfits);
                Assert.False(comparison.ModifiesRace);
            });
        }

        [Fact]
        public void ProvidesComparisonsForAllMasters_InListingOrder()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin1.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                npc.FaceMorph.BrowsForwardVsBack = -0.1f;
                npc.Height = 0.99f;
            }));
            Groups.AddRecords<Npc>("plugin2.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var hairColorKeys = Groups.AddRecords<ColorRecord>("plugin2.esp", x => { });
                npc.HairColor.SetTo(hairColorKeys[0].ToFormKey());
            }));
            Groups.AddRecords<Npc>("plugin3.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                npc.Configuration.DispositionBase = 2;
            }));
            Groups.AddRecords<Npc>("plugin4.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                var classKeys = Groups.AddRecords<Class>("plugin4.esp", x => { });
                npc.Class.SetTo(classKeys[0].ToFormKey());
            }));
            Groups.AddRecords<Npc>("analyze.esp", "master.esp", x => SetupNpcForComparison(x, npc =>
            {
                npc.FaceMorph.BrowsForwardVsBack = -0.1f;
            }));
            Groups.ConfigureMod("analyze.esp", modMock =>
            {
                var modHeaderMock = new Mock<ISkyrimModHeaderGetter>();
                modHeaderMock.SetupGet(x => x.MasterReferences).Returns(new ExtendedList<MasterReference>(new[]
                {
                    new MasterReference { Master = "master.esp" },
                    new MasterReference { Master = "plugin1.esp" },
                    new MasterReference { Master = "plugin2.esp" },
                    // Skip plugin3 just to test that this only includes actual masters - not all previous overrides.
                    new MasterReference { Master = "plugin4.esp" },
                }));
                modMock.SetupGet(x => x.ModHeader).Returns(modHeaderMock.Object);
            });
            var analysis = Analyzer.Analyze("analyze.esp", npcKeys[0]);

            Assert.Collection(
                analysis.ComparisonToMasters,
                x =>
                {
                    Assert.Equal("master.esp", x.PluginName);
                    Assert.False(x.IsIdentical);
                    Assert.False(x.ModifiesBehavior);
                    Assert.True(x.ModifiesFace);
                    Assert.False(x.ModifiesScales);
                },
                x =>
                {
                    Assert.Equal("plugin1.esp", x.PluginName);
                    Assert.False(x.IsIdentical);
                    Assert.False(x.ModifiesBehavior);
                    Assert.False(x.ModifiesFace);
                    Assert.True(x.ModifiesScales);
                },
                x =>
                {
                    Assert.Equal("plugin2.esp", x.PluginName);
                    Assert.False(x.IsIdentical);
                    Assert.False(x.ModifiesBehavior);
                    Assert.True(x.ModifiesFace);
                    Assert.False(x.ModifiesScales);
                },
                x =>
                {
                    Assert.Equal("plugin4.esp", x.PluginName);
                    Assert.False(x.IsIdentical);
                    Assert.True(x.ModifiesBehavior);
                    Assert.True(x.ModifiesFace);
                    Assert.False(x.ModifiesScales);
                });
        }

        // Special case of adding records that are really just markers, i.e. don't play a role in any logic we want to
        // test except by virtue of their presence/absence in a reference.
        // The editor ID just helps for debugging in the event of a test failure.
        private IReadOnlyList<FormKey> AddEmptyRecords<T>(string pluginName, params string[] editorIds)
            where T : class, ISkyrimMajorRecord
        {
            return Groups
                .AddRecords(pluginName, editorIds.Select(id => (Action<T>)((T x) => x.EditorID = id)).ToArray())
                .ToFormKeys()
                .ToList()
                .AsReadOnly();
        }

        private void AssertComparisons(
            NpcAnalysis analysis, string masterPluginName, string previousOverridePluginName,
            Action<NpcComparison> comparisonAssert)
        {
            Assert.Equal(masterPluginName, analysis.ComparisonToBase.PluginName);
            comparisonAssert(analysis.ComparisonToBase);
            Assert.Equal(previousOverridePluginName, analysis.ComparisonToPreviousOverride.PluginName);
            comparisonAssert(analysis.ComparisonToPreviousOverride);
        }

        private static HeadData CreateHeadData(IEnumerable<FormKey> headPartKeys)
        {
            var headData = new HeadData();
            var headPartRefs = headPartKeys.Select((x, i) =>
            {
                var hpRef = new HeadPartReference { Number = i };
                hpRef.Head.SetTo(x);
                return hpRef;
            });
            headData.HeadParts.AddRange(headPartRefs);
            return headData;
        }

        private static GenderedItem<HeadData> CreateHeadData(IEnumerable<FormKey> headPartKeys, bool female)
        {
            var headData = CreateHeadData(headPartKeys);
            return female ? new GenderedItem<HeadData>(null, headData) : new GenderedItem<HeadData>(headData, null);
        }

        private FormKey GetDefaultHeadPartKey(IRaceGetter race, bool female, HeadPart.TypeEnum type)
        {
            var headData = female ? race.HeadData.Female : race.HeadData.Male;
            return headData.HeadParts
                .Select(x => x.Head)
                .SingleOrDefault(x => x.WinnerFrom(Groups).Type == type)
                .FormKey;
        }

        private void SetupNpcComparisonDependencies()
        {
            // NPCs need to reference a lot of other stuff in order to fully test comparisons - factions, races, voices,
            // and so on. These have to be added to the "global" cache, and reused on multiple NPCs to start with the
            // same baseline, but we need to only set up the dependencies one time (vs. NPCs which are set up multiple
            // times).
            if (comparisonDependencies != null)
                return;
            const string pluginName = "vanilla.esp";
            var raceSkinKey = AddEmptyRecords<Armor>(pluginName, "SkinNaked").Single();
            comparisonDependencies = new()
            {
                ClassKey = AddEmptyRecords<Class>(pluginName, "Citizen").Single(),
                CombatStyleKey = AddEmptyRecords<CombatStyle>(pluginName, "HumanMelee").Single(),
                CrimeFactionKey = AddEmptyRecords<Faction>(pluginName, "CrimeFactionWatford").Single(),
                DefaultOutfitKey = AddEmptyRecords<Outfit>(pluginName, "MerchantClothes1").Single(),
                FactionRanks =
                    AddEmptyRecords<Faction>(pluginName, "JarlFaction", "VampireFaction", "MarriageExcludedFaction")
                    // Faction ranks appear signed in xEdit, but Mutagen uses a byte type?
                    .Zip(new byte[] { 1, 0, 2 })
                    .Select(x => new RankPlacement { Faction = x.First.ToLink<IFactionGetter>(), Rank = x.Second })
                    .ToList()
                    .AsReadOnly(),
                HairColorKey = AddEmptyRecords<ColorRecord>(pluginName, "Blond").Single(),
                HeadPartKeys = Groups.AddRecords<HeadPart>(
                    pluginName,
                    x => { x.EditorID = "FemaleEyesHumanBlue"; x.Type = HeadPart.TypeEnum.Eyes; },
                    x => { x.EditorID = "HairFemaleNord5"; x.Type = HeadPart.TypeEnum.Hair; },
                    x => { x.EditorID = "BrowsFemaleHuman2"; x.Type = HeadPart.TypeEnum.Eyebrows; })
                    .ToFormKeys().ToList().AsReadOnly(),
                HeadTextureKey = AddEmptyRecords<TextureSet>(pluginName, "SkinHeadFemaleNord").Single(),
                ItemEntries = AddEmptyRecords<MiscItem>(pluginName, "Basket", "Health Potion", "Gold")
                    .Zip(new[] { 1, 5, 50 })
                    .Select(x => new ContainerEntry {
                        Item = new ContainerItem { Item = x.First.ToLink<IMiscItemGetter>(), Count = x.Second }
                    })
                    .ToList()
                    .AsReadOnly(),
                KeywordKeys = AddEmptyRecords<Keyword>(pluginName, "JobJarl", "TraitPowerHungry"),
                PackageKeys = AddEmptyRecords<Package>(pluginName, "WatfordWork", "DefaultSandbox", "Eat", "Sing"),
                PerkPlacements = AddEmptyRecords<Perk>(pluginName, "MagicResistance30", "Recovery30", "Regeneration")
                    .Zip(new byte[] { 2, 3, 1 })
                    .Select(x => new PerkPlacement { Perk = x.First.ToLink<IPerkGetter>(), Rank = x.Second })
                    .ToList()
                    .AsReadOnly(),
                SleepOutfitKey = AddEmptyRecords<Outfit>(pluginName, "DefaultSleepOutfit").Single(),
                RaceKey = Groups.AddRecords<Race>(pluginName, r =>
                    {
                        r.EditorID = "NordRace";
                        r.HeadData = new GenderedItem<HeadData>(
                            CreateHeadData(Groups.AddRecords<HeadPart>(
                                pluginName,
                                x => { x.EditorID = "MaleHeadNord"; x.Type = HeadPart.TypeEnum.Face; },
                                x => { x.EditorID = "MaleEyesHumanBrown"; x.Type = HeadPart.TypeEnum.Eyes; },
                                x => { x.EditorID = "HairMaleNord1"; x.Type = HeadPart.TypeEnum.Hair; },
                                x => { x.EditorID = "BrowsMaleHuman1"; x.Type = HeadPart.TypeEnum.Eyebrows; })
                            .ToFormKeys()),
                            CreateHeadData(Groups.AddRecords<HeadPart>(
                                pluginName,
                                x => { x.EditorID = "FemaleHeadNord"; x.Type = HeadPart.TypeEnum.Face; },
                                x => { x.EditorID = "FemaleEyesHumanBrown"; x.Type = HeadPart.TypeEnum.Eyes; },
                                x => { x.EditorID = "HairFemaleNord1"; x.Type = HeadPart.TypeEnum.Hair; },
                                x => { x.EditorID = "BrowsFemaleHuman1"; x.Type = HeadPart.TypeEnum.Eyebrows; })
                            .ToFormKeys()));
                        r.Skin.SetTo(raceSkinKey);
                    }).Single().ToFormKey(),
                RaceSkinKey = raceSkinKey,
                VoiceKey = AddEmptyRecords<VoiceType>(pluginName, "FemaleSultry").Single(),
            };
        }

        private void SetupNpcForComparison(Npc npc)
        {
            SetupNpcForComparison(npc, null);
        }

        private void SetupNpcForComparison(Npc npc, Action<Npc> mutation)
        {
            SetupNpcComparisonDependencies();
            // Tests for the comparisons, and by extension the defaults being set up here, are not going to be
            // completely exhaustive and cover every single possible property. They are intended to provide a reasonable
            // sample to use as a baseline for data-driven tests, which can mutate one property (or several) to study
            // its impact on the result.
            //
            // As bugs are inevitably discovered in the wild, more cases may need to be added.
            npc.EditorID = "ComparerNpc";

            npc.AIData = new AIData
            {
                Aggression = Aggression.Unagressive,
                AggroRadiusBehavior = true,
                Assistance = Assistance.HelpsAllies,
                Attack = 5,
                Confidence = Confidence.Brave,
                EnergyLevel = 60,
                Mood = Mood.Happy,
                Responsibility = Responsibility.ViolenceAgainstEnemies,
                Warn = 50,
                WarnOrAttack = 20,
            };
            npc.Class.SetTo(comparisonDependencies.ClassKey);
            npc.CombatStyle.SetTo(comparisonDependencies.CombatStyleKey);
            npc.Configuration = new()
            {
                Flags = NpcConfiguration.Flag.Female | NpcConfiguration.Flag.Unique | NpcConfiguration.Flag.Essential,
                CalcMaxLevel = 100,
                CalcMinLevel = 5,
                Level = new NpcLevel { Level = 15 },
                HealthOffset = 20,
                MagickaOffset = -25,
                StaminaOffset = 10,
                SpeedMultiplier = 98,
            };
            npc.CrimeFaction.SetTo(comparisonDependencies.CrimeFactionKey);
            npc.DefaultOutfit.SetTo(comparisonDependencies.DefaultOutfitKey);
            npc.FaceMorph = new NpcFaceMorph
            {
                BrowsForwardVsBack = -0.4f,
                BrowsInVsOut = -0.1f,
                BrowsUpVsDown = -0.5f,
                CheeksForwardVsBack = -0.3f,
                CheeksUpVsDown = 0.9f,
                ChinNarrowVsWide = 0f,
                ChinUnderbiteVsOverbite = -0.1f,
                ChinUpVsDown = 0.8f,
                EyesForwardVsBack = -0.6f,
                EyesInVsOut = -0.7f,
                EyesUpVsDown = 1.0f,
                JawForwardVsBack = 0.3f,
                JawNarrowVsWide = -0.8f,
                JawUpVsDown = 0.2f,
                LipsInVsOut = 0.5f,
                LipsUpVsDown = -0.2f,
                NoseLongVsShort = -0.9f,
                NoseUpVsDown = 0.6f,
                Unknown = 0.1f,
            };
            npc.FaceParts = new NpcFaceParts
            {
                Eyes = 1,
                Mouth = 6,
                Unknown = 4,
            };
            npc.Factions.AddRange(comparisonDependencies.FactionRanks);
            npc.HairColor.SetTo(comparisonDependencies.HairColorKey);
            npc.HeadParts.AddRange(comparisonDependencies.HeadPartKeys);
            npc.HeadTexture.SetTo(comparisonDependencies.HeadTextureKey);
            npc.Height = 1.01f;
            npc.Items = new(comparisonDependencies.ItemEntries);
            npc.Keywords = new();
            npc.Keywords.AddRange(comparisonDependencies.KeywordKeys);
            npc.Name = "Example NPC";
            npc.ObjectBounds = new()
            {
                First = new P3Int16(-12, -16, 0),
                Second = new P3Int16(41, 17, 96),
            };
            npc.Packages.AddRange(comparisonDependencies.PackageKeys);
            npc.Perks = new(comparisonDependencies.PerkPlacements);
            npc.PlayerSkills = new PlayerSkills
            {
                Health = 120,
                Magicka = 150,
                Stamina = 90,
                SkillOffsets =
                {
                    { Skill.Alchemy, 0 },
                    { Skill.Alteration, 0 },
                    { Skill.Archery, 3 },
                    { Skill.Block, 1 },
                    { Skill.Conjuration, 4 },
                    { Skill.Destruction, 4 },
                    { Skill.Enchanting, 0 },
                    { Skill.HeavyArmor, 0 },
                    { Skill.Illusion, 1 },
                    { Skill.LightArmor, 3 },
                    { Skill.Lockpicking, 4 },
                    { Skill.OneHanded, 3 },
                    { Skill.Pickpocket, 1 },
                    { Skill.Restoration, 4 },
                    { Skill.Smithing, 5 },
                    { Skill.Sneak, 2 },
                    { Skill.Speech, 3 },
                    { Skill.TwoHanded, 5 },
                },
                SkillValues =
                {
                    { Skill.Alchemy, 30 },
                    { Skill.Alteration, 25 },
                    { Skill.Archery, 18 },
                    { Skill.Block, 10 },
                    { Skill.Conjuration, 26 },
                    { Skill.Destruction, 15 },
                    { Skill.Enchanting, 22 },
                    { Skill.HeavyArmor, 28 },
                    { Skill.Illusion, 12 },
                    { Skill.LightArmor, 13 },
                    { Skill.Lockpicking, 21 },
                    { Skill.OneHanded, 19 },
                    { Skill.Pickpocket, 29 },
                    { Skill.Restoration, 11 },
                    { Skill.Smithing, 20 },
                    { Skill.Sneak, 23 },
                    { Skill.Speech, 17 },
                    { Skill.TwoHanded, 14 },
                },
            };
            npc.Race.SetTo(comparisonDependencies.RaceKey);
            npc.ShortName = "Comparer NPC";
            npc.SleepingOutfit.SetTo(comparisonDependencies.SleepOutfitKey);
            npc.TextureLighting = Color.GhostWhite;
            npc.TintLayers.AddRange(new[]
            {
                new TintLayer { Index = 26, Color = Color.IndianRed, InterpolationValue = 0.3f },
                new TintLayer { Index = 27, Color = Color.Indigo, InterpolationValue = 0.67f },
                new TintLayer { Index = 28, Color = Color.Pink, InterpolationValue = 0.89f },
                new TintLayer { Index = 29, Color = Color.Plum, InterpolationValue = 0.5f },
            });
            npc.VirtualMachineAdapter = new VirtualMachineAdapter
            {
                ObjectFormat = 2,
                Version = 5,
                Scripts =
                {
                    new ScriptEntry
                    {
                        Name = "ExampleScript1",
                        Flags = ScriptEntry.Flag.Local,
                        Properties =
                        {
                            new ScriptIntProperty
                            {
                                Name = "Script1_Prop1",
                                Flags = ScriptProperty.Flag.Edited,
                                Data = 38
                            },
                            new ScriptStringProperty
                            {
                                Name = "Script1_Prop2",
                                Flags = ScriptProperty.Flag.Edited,
                                Data = "test value",
                            },
                        },
                    },
                    new ScriptEntry
                    {
                        Name = "ExampleScript2",
                        Flags = ScriptEntry.Flag.Inherited,
                        Properties =
                        {
                            new ScriptIntListProperty
                            {
                                Name = "Script2_Prop1",
                                Flags = ScriptProperty.Flag.Removed,
                                Data = { 1, 2, 3, 4, 5 },
                            }
                        },
                    },
                },
            };
            npc.Voice.SetTo(comparisonDependencies.VoiceKey);
            npc.Weight = 60.0f;
            mutation?.Invoke(npc);
        }

        private void SetupPositiveWigScenario(Npc npc, string editorId, Action<ArmorAddon> additionalSetup = null)
        {
            SetupPositiveWigScenario(npc, null, editorId, additionalSetup);
        }

        private void SetupPositiveWigScenario(
            Npc npc, FormKey? raceKey, string editorId, Action<ArmorAddon> additionalSetup = null)
        {
            SetupWigScenario(raceKey, npc, new WigSetting
            {
                AdditionalSetup = additionalSetup,
                EditorId = editorId,
                ReplacesHair = true,
                SupportsRace = true,
                IsDefaultSkin = false
            });
        }

        private void SetupWigScenario(Npc npc, params WigSetting[] addonConfigs)
        {
            SetupWigScenario(null, npc, addonConfigs);
        }

        private void SetupWigScenario(FormKey? raceKey, Npc npc, params WigSetting[] addonConfigs)
        {
            if (raceKey == null)
            {
                raceKey = Groups.AddRecords<Race>("plugin.esp", _ => { })[0].ToFormKey();
                npc.Race.SetTo(raceKey);
            }
            var addonKeys = CreateAddons().ToList();
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(addonKeys));
            npc.WornArmor.SetTo(armorKeys[0].ToFormKey());

            IEnumerable<FormKey> CreateAddons()
            {
                foreach (var cfg in addonConfigs)
                {
                    var addonKeys = Groups.AddRecords<ArmorAddon>("plugin.esp", x =>
                    {
                        x.EditorID = cfg.EditorId;
                        cfg.AdditionalSetup?.Invoke(x);
                    });
                    var key = addonKeys[0].ToFormKey();
                    armorAddonHelperMock
                        .Setup(x => x.ReplacesHair(It.Is<IArmorAddonGetter>(x => x.FormKey == key)))
                        .Returns(cfg.ReplacesHair);
                    armorAddonHelperMock
                        .Setup(x => x.SupportsRace(
                            It.Is<IArmorAddonGetter>(x => x.FormKey == key),
                            It.Is<IFormLinkGetter<IRaceGetter>>(x => x.FormKey == raceKey)))
                        .Returns(cfg.SupportsRace);
                    armorAddonHelperMock
                        .Setup(x => x.IsDefaultSkin(
                            It.Is<IArmorAddonGetter>(x => x.FormKey == key),
                            It.Is<IFormLinkGetter<IRaceGetter>>(x => x.FormKey == raceKey)))
                        .Returns(cfg.IsDefaultSkin);
                    yield return key;
                }
            }
        }

        class NpcComparisonDependencies
        {
            public FormKey ClassKey { get; init; }
            public FormKey CombatStyleKey { get; init; }
            public FormKey CrimeFactionKey { get; init; }
            public FormKey DefaultOutfitKey { get; init; }
            public IReadOnlyList<RankPlacement> FactionRanks { get; init; }
            public FormKey HairColorKey { get; init; }
            public IReadOnlyList<FormKey> HeadPartKeys { get; init; }
            public FormKey HeadTextureKey { get; init; }
            public IReadOnlyList<ContainerEntry> ItemEntries { get; init; }
            public IReadOnlyList<FormKey> KeywordKeys { get; init; }
            public IReadOnlyList<FormKey> PackageKeys { get; init; }
            public IReadOnlyList<PerkPlacement> PerkPlacements { get; init; }
            public FormKey SleepOutfitKey { get; init; }
            public FormKey RaceKey { get; init; }
            public FormKey RaceSkinKey { get; init; }
            public FormKey VoiceKey { get; init; }
        }

        class WigSetting
        {
            public Action<ArmorAddon> AdditionalSetup { get; init; }
            public string EditorId { get; init; }
            public bool IsDefaultSkin { get; init; }
            public bool ReplacesHair { get; init; }
            public bool SupportsRace { get; init; }
        }
    }
}

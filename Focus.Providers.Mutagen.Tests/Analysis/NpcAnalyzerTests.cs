using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class NpcAnalyzerTests : CommonAnalyzerFacts<NpcAnalyzer, Npc, NpcAnalysis>
    {
        public NpcAnalyzerTests()
        {
            Analyzer = new NpcAnalyzer(Groups, Logger);
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
                x => { x.EditorID = "ModdedHair"; x.Type = HeadPart.TypeEnum.Hair; });
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
        public void WhenWornArmorHasNoHairExclusiveAddons_WigInfo_IsNull()
        {
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Body },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hands },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair | BipedObjectFlag.Ears });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.WornArmor.SetTo(armorKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Null(analysis.WigInfo);
        }

        [Fact]
        public void WhenWornArmorHasHairAddon_WigInfo_ReferencesMatchingAddon()
        {
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Body },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hands },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.WornArmor.SetTo(armorKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(armatureKeys[2], analysis.WigInfo.Key);
        }

        [Fact]
        public void WhenWornArmorHasLongHairAddon_WigInfo_ReferencesMatchingAddon()
        {
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Body },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.LongHair },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hands });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.WornArmor.SetTo(armorKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(armatureKeys[1], analysis.WigInfo.Key);
        }

        [Fact]
        public void WhenWornArmorHasHairAndLongHairAddon_WigInfo_ReferencesMatchingAddon()
        {
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Body },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair | BipedObjectFlag.LongHair },
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hands });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x => x.WornArmor.SetTo(armorKeys[0].ToFormKey()));
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal(armatureKeys[1], analysis.WigInfo.Key);
        }

        [Fact]
        public void WhenNpcHasWig_AndNoHairInHeadParts_WigInfo_IsBald()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>(
                "plugin.esp",
                x => { x.Type = HeadPart.TypeEnum.Face; x.Model = new() { File = "face.nif" }; },
                x => { x.Type = HeadPart.TypeEnum.Eyes; x.Model = new() { File = "eyes.nif" }; });
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.HeadParts.AddRange(headPartKeys.ToFormKeys());
                x.WornArmor.SetTo(armorKeys[0].ToFormKey());
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
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.Race.SetTo(raceKeys[0].ToFormKey());
                x.HeadParts.Add(headPartKeys[1].ToFormKey());
                x.WornArmor.SetTo(armorKeys[0].ToFormKey());
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
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x => x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>("plugin.esp", x =>
            {
                x.Race.SetTo(raceKeys[0].ToFormKey());
                x.WornArmor.SetTo(armorKeys[0].ToFormKey());
            });
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.False(analysis.WigInfo.IsBald);
        }

        [Fact]
        public void WhenNpcHasWig_AndWigHasWorldModel_WigInfo_IncludesGenderedModelName()
        {
            var armatureKeys = Groups.AddRecords<ArmorAddon>(
                "plugin.esp",
                x =>
                {
                    x.BodyTemplate = new() { FirstPersonFlags = BipedObjectFlag.Hair };
                    x.WorldModel = new GenderedItem<Model>(
                        new() { File = @"foo/bar/baz/wigmodelmale.nif" },
                        new() { File = @"foo/bar/baz/wigmodelfemale.nif" });
                });
            var armorKeys = Groups.AddRecords<Armor>("plugin.esp", x => x.Armature.AddRange(armatureKeys.ToFormKeys()));
            var npcKeys = Groups.AddRecords<Npc>(
                "plugin.esp",
                x => x.WornArmor.SetTo(armorKeys[0].ToFormKey()),
                x => { x.Configuration.Flags |= NpcConfiguration.Flag.Female; x.WornArmor.SetTo(armorKeys[0].ToFormKey()); });
            var maleAnalysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);
            var femaleAnalysis = Analyzer.Analyze("plugin.esp", npcKeys[1]);

            Assert.Equal("wigmodelmale", maleAnalysis.WigInfo.ModelName);
            Assert.Equal("wigmodelfemale", femaleAnalysis.WigInfo.ModelName);
        }

        private static GenderedItem<HeadData> CreateHeadData(IEnumerable<FormKey> headPartKeys, bool female)
        {
            var headData = new HeadData();
            var headPartRefs = headPartKeys.Select((x, i) =>
            {
                var hpRef = new HeadPartReference { Number = i };
                hpRef.Head.SetTo(x);
                return hpRef;
            });
            headData.HeadParts.AddRange(headPartRefs);
            return female ? new GenderedItem<HeadData>(null, headData) : new GenderedItem<HeadData>(headData, null);
        }
    }
}

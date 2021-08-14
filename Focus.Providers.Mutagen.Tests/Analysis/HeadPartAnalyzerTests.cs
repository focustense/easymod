using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class HeadPartAnalyzerTests : CommonAnalyzerFacts<HeadPartAnalyzer, HeadPart, HeadPartAnalysis>
    {
        public static IEnumerable<object[]> RaceExtractionData => new[]
        {
            new object[]
            {
                new string[] { "NordRace", "ImperialRace", "RedguardRace", "ElderRace", },
                new VanillaRace[] { VanillaRace.Nord, VanillaRace.Imperial, VanillaRace.Redguard, VanillaRace.Elder },
            },
            new object[] { new [] { "BretonRace" }, new[] { VanillaRace.Breton } },
            new object[] { new [] { "HighElfRace" }, new[] { VanillaRace.HighElf } },
            new object[] { new [] { "DarkElfRace" }, new[] { VanillaRace.DarkElf } },
            new object[]
            {
                new string[] { "WoodElfRace", "OrcRace", "UnknownRace" },
                new VanillaRace[] { VanillaRace.WoodElf, VanillaRace.Orc },
            },
        };

        public HeadPartAnalyzerTests()
        {
            Analyzer = new HeadPartAnalyzer(Groups);
        }

        [Theory]
        [InlineData((HeadPart.Flag)0, true, true)]
        [InlineData(HeadPart.Flag.Female, true, false)]
        [InlineData(HeadPart.Flag.Male, false, true)]
        [InlineData(HeadPart.Flag.Female | HeadPart.Flag.Male, true, true)]
        public void AppliesSexFlags(HeadPart.Flag flags, bool expectedFemale, bool expectedMale)
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp => hp.Flags = flags);
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.Equal(expectedFemale, analysis.SupportsFemaleNpcs);
            Assert.Equal(expectedMale, analysis.SupportsMaleNpcs);
        }

        [Theory]
        [InlineData(HeadPart.TypeEnum.Eyebrows, HeadPartType.Eyebrows)]
        [InlineData(HeadPart.TypeEnum.Eyes, HeadPartType.Eyes)]
        [InlineData(HeadPart.TypeEnum.Face, HeadPartType.Face)]
        [InlineData(HeadPart.TypeEnum.FacialHair, HeadPartType.FacialHair)]
        [InlineData(HeadPart.TypeEnum.Hair, HeadPartType.Hair)]
        [InlineData(HeadPart.TypeEnum.Misc, HeadPartType.Misc)]
        [InlineData(HeadPart.TypeEnum.Scars, HeadPartType.Scars)]
        [InlineData(null, HeadPartType.Unknown)]
        public void ConvertsHeadPartType(HeadPart.TypeEnum? sourceType, HeadPartType destType)
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp => hp.Type = sourceType);
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.Equal(destType, analysis.PartType);
        }

        [Theory]
        [MemberData(nameof(RaceExtractionData))]
        public void ExtractsValidVanillaRacesFromEditorIds(IEnumerable<string> raceEditorIds, IEnumerable<VanillaRace> resolvedRaces)
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp =>
            {
                var raceKeys = Groups.AddRecords(
                    "mod.esp",
                    raceEditorIds.Select(editorId => (Action<Race>)(r => r.EditorID = editorId)).ToArray());
                var raceListKeys = Groups.AddRecords<FormList>("mod.esp", f => f.Items.AddRange(raceKeys.ToFormKeys()));
                hp.ValidRaces.SetTo(raceListKeys[0].ToFormKey());
            });
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.Equal(resolvedRaces, analysis.ValidVanillaRaces);
        }

        [Fact]
        public void IncludesExtraParts()
        {
            var extraPartKeys = Groups.AddRecords<HeadPart>("mod.esp", x => { }, x => { });
            var headPartKeys = Groups.AddRecords<HeadPart>(
                "mod.esp", hp => hp.ExtraParts.AddRange(extraPartKeys.ToFormKeys()));
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.Equal(extraPartKeys, analysis.ExtraPartKeys);
        }

        [Fact]
        public void IncludesModelName()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp => hp.Model = new() { File = "dir/model.nif" });
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.Equal("dir/model.nif", analysis.ModelFileName);
        }

        [Fact]
        public void WhenHasExtraPartFlag_IsNotMainPart()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp => hp.Flags |= HeadPart.Flag.IsExtraPart);
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.False(analysis.IsMainPart);
        }

        [Fact]
        public void WhenNoExtraPartFlag_IsMainPart()
        {
            var headPartKeys = Groups.AddRecords<HeadPart>("mod.esp", hp => { });
            var analysis = Analyzer.Analyze("mod.esp", headPartKeys[0]);

            Assert.True(analysis.IsMainPart);
        }
    }
}

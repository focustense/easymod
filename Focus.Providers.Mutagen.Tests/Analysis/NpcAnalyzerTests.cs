﻿using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
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
        private NpcComparisonDependencies comparisonDependencies;

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

        [Fact]
        public void WhenIdenticalToMaster_AllComparisons_AreTrue()
        {
            var npcKeys = Groups.AddRecords<Npc>("master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("override.esp", "master.esp", SetupNpcForComparison);
            Groups.AddRecords<Npc>("plugin.esp", "master.esp", SetupNpcForComparison);
            var analysis = Analyzer.Analyze("plugin.esp", npcKeys[0]);

            Assert.Equal("master.esp", analysis.ComparisonToMaster.PluginName);
            Assert.True(analysis.ComparisonToMaster.IsIdentical);
            Assert.False(analysis.ComparisonToMaster.ModifiesBehavior);
            Assert.False(analysis.ComparisonToMaster.ModifiesBody);
            Assert.False(analysis.ComparisonToMaster.ModifiesFace);
            Assert.False(analysis.ComparisonToMaster.ModifiesHair);
            Assert.False(analysis.ComparisonToMaster.ModifiesHeadParts);
            Assert.False(analysis.ComparisonToMaster.ModifiesOutfits);
            Assert.False(analysis.ComparisonToMaster.ModifiesRace);

            Assert.Equal("override.esp", analysis.ComparisonToPreviousOverride.PluginName);
            Assert.True(analysis.ComparisonToPreviousOverride.IsIdentical);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesBehavior);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesBody);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesFace);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesHair);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesHeadParts);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesOutfits);
            Assert.False(analysis.ComparisonToPreviousOverride.ModifiesRace);
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

        private void SetupNpcComparisonDependencies()
        {
            // NPCs need to reference a lot of other stuff in order to fully test comparisons - factions, races, voices,
            // and so on. These have to be added to the "global" cache, and reused on multiple NPCs to start with the
            // same baseline, but we need to only set up the dependencies one time (vs. NPCs which are set up multiple
            // times).
            if (comparisonDependencies != null)
                return;
            const string pluginName = "vanilla.esp";
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
                    .Select(x => new RankPlacement { Faction = x.First.AsLink<IFactionGetter>(), Rank = x.Second })
                    .ToList()
                    .AsReadOnly(),
                HairColorKey = AddEmptyRecords<ColorRecord>(pluginName, "Blond").Single(),
                HeadPartKeys = Groups.AddRecords<HeadPart>(
                    pluginName,
                    x => { x.EditorID = "FemaleEyesHumanBlue"; },
                    x => { x.EditorID = "HairFemaleNord5"; }).ToFormKeys().ToList().AsReadOnly(),
                HeadTextureKey = AddEmptyRecords<TextureSet>(pluginName, "SkinHeadFemaleNord").Single(),
                ItemEntries = AddEmptyRecords<MiscItem>(pluginName, "Basket", "Health Potion", "Gold")
                    .Zip(new[] { 1, 5, 50 })
                    .Select(x => new ContainerEntry {
                        Item = new ContainerItem { Item = x.First.AsLink<IMiscItemGetter>(), Count = x.Second }
                    })
                    .ToList()
                    .AsReadOnly(),
                KeywordKeys = AddEmptyRecords<Keyword>(pluginName, "JobJarl", "TraitPowerHungry"),
                PackageKeys = AddEmptyRecords<Package>(pluginName, "WatfordWork", "DefaultSandbox"),
                PerkPlacements = AddEmptyRecords<Perk>(pluginName, "MagicResistance30", "Recovery30", "Regeneration")
                    .Zip(new byte[] { 2, 3, 1 })
                    .Select(x => new PerkPlacement { Perk = x.First.AsLink<IPerkGetter>(), Rank = x.Second })
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
                        r.Skin.SetTo(AddEmptyRecords<Armor>(pluginName, "SkinNaked").Single());
                    }).Single().ToFormKey(),
                VoiceKey = AddEmptyRecords<VoiceType>(pluginName, "FemaleSultry").Single(),
            };
        }

        private void SetupNpcForComparison(Npc npc)
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
            // Attacks? Hard to find these on NPCs, usually just on race.
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
                new TintLayer { Index = 26, Color = Color.IndianRed },
                new TintLayer { Index = 27, Color = Color.Indigo },
                new TintLayer { Index = 28, Color = Color.Pink },
                new TintLayer { Index = 29, Color = Color.Plum },
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
            public FormKey VoiceKey { get; init; }
        }
    }
}
using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class ReferenceCheckerTests
    {
        private readonly IReferenceChecker<INpcGetter> checker;
        private readonly FakeGroupCache groups = new();

        private IReferenceFollower<INpcGetter> follower;

        public ReferenceCheckerTests()
        {
            checker = new ReferenceChecker<INpcGetter>(groups)
                .Configure(f => follower = f
                    .Follow(x => x.Class)
                    .Follow(x => x.DefaultOutfit)
                    .Follow(x => x.Keywords)
                    .Follow(x => x.HeadParts, headPart => headPart
                        .Follow(x => x.Color)
                        .FollowSelf(x => x.ExtraParts)
                        .Follow(x => x.TextureSet))
                    .Follow(x => x.Race, race => race
                        .Follow(x => x.DefaultHairColors)
                        .Follow(x => x.Skin, skin => skin
                            .Follow(x => x.Armature, addon => addon
                                .Follow(x => x.AdditionalRaces)
                                .Follow(x => x.WorldModel, g => g.AlternateTextures?.Select(t => t.NewTexture))))));
        }

        [Fact]
        public void WhenAllReferencesValid_YieldsNoResults()
        {
            var classKey = AddRecord<Class>("DefaultClass");
            var keywordKeys = AddRecords<Keyword>("Keyword1", "Keyword2");
            var headPartKeys = groups
                .AddRecords<HeadPart>(
                    "main.esp",
                    x =>
                    {
                        x.EditorID = "Hair";
                        x.Color.SetTo(AddRecord<ColorRecord>("HairColor"));
                        x.ExtraParts.AddRange(groups.AddRecords<HeadPart>("main.esp", hp =>
                        {
                            hp.EditorID = "Hairline";
                            hp.Color.SetTo(AddRecord<ColorRecord>("HairlineColor"));
                        }).ToFormKeys());
                        x.TextureSet.SetTo(AddRecord<TextureSet>("HairTextureSet"));
                    },
                    x =>
                    {
                        x.EditorID = "Face";
                        x.Color.SetTo(AddRecord<ColorRecord>("FaceColor"));
                    })
                .ToFormKeys();
            var raceKey = groups
                .AddRecords<Race>(
                    "main.esp",
                    x =>
                    {
                        x.EditorID = "DefaultRace";
                        x.DefaultHairColors = new GenderedItem<IFormLinkGetter<IColorRecordGetter>>(
                            AddRecord<ColorRecord>("DefaultMaleHairColor").AsLink<IColorRecordGetter>(),
                            AddRecord<ColorRecord>("DefaultFemaleHairColor").AsLink<IColorRecordGetter>());
                        x.Skin = groups
                            .AddRecords<Armor>(
                                "main.esp",
                                a =>
                                {
                                    a.EditorID = "BodyArmor";
                                    a.Armature.AddRange(groups.AddRecords<ArmorAddon>(
                                        "main.esp",
                                        d =>
                                        {
                                            d.EditorID = "BodyAddon";
                                            d.AdditionalRaces.AddRange(AddRecords<Race>("AddRace1", "AddRace2"));
                                        },
                                        d =>
                                        {
                                            d.EditorID = "FeetAddon";
                                            var maleModel = new Model();
                                            var femaleModel = new Model
                                            {
                                                AlternateTextures = new(new[] {
                                                    new AlternateTexture
                                                    {
                                                        NewTexture = AddRecord<TextureSet>("Feet1")
                                                            .AsLink<ITextureSetGetter>()
                                                    },
                                                    new AlternateTexture
                                                    {
                                                        NewTexture = AddRecord<TextureSet>("Feet2")
                                                            .AsLink<ITextureSetGetter>()
                                                    },
                                                }),
                                            };
                                            d.WorldModel = new GenderedItem<Model>(maleModel, femaleModel);
                                        }).ToFormKeys());
                                })
                            .Single()
                            .ToFormKey()
                            .AsNullableLink<IArmorGetter>();
                    })
                .Single()
                .ToFormKey();
            var npc = new Npc(FormKey.Factory("123456:main.esp"), SkyrimRelease.SkyrimSE)
            {
                Class = classKey.AsLink<IClassGetter>(),
                HeadParts = new(headPartKeys.Select(x => x.AsLink<IHeadPartGetter>())),
                Keywords = new(keywordKeys.Select(x => x.AsLink<IKeywordGetter>())),
                Race = raceKey.AsLink<IRaceGetter>()
            };
            var invalidPaths = checker.GetInvalidPaths(npc);

            Assert.Empty(invalidPaths);
        }

        [Fact]
        public void WhenReferencesInvalid_YieldsInvalidPaths()
        {
            var classKey = FormKey.Factory("123456:badclass.esp");
            var keywordKeys = new[]
            {
                AddRecord<Keyword>("Keyword1"),
                FormKey.Factory("123456:badkeyword.esp"),
                AddRecord<Keyword>("Keyword2"),
            };
            var headPartKeys = groups
                .AddRecords<HeadPart>(
                    "main.esp",
                    x =>
                    {
                        x.EditorID = "Hair";
                        x.Color.SetTo(AddRecord<ColorRecord>("HairColor"));
                        x.ExtraParts.AddRange(groups.AddRecords<HeadPart>("main.esp", hp =>
                        {
                            hp.EditorID = "Hairline";
                            hp.Color.SetTo(AddRecord<ColorRecord>("HairlineColor"));
                            hp.TextureSet.SetTo(FormKey.Factory("123456:badhairlinetextureset.esp"));
                        }).ToFormKeys());
                        x.ExtraParts.Add(FormKey.Factory("123456:badheadpart.esp").AsLink<IHeadPartGetter>());
                        x.TextureSet.SetTo(AddRecord<TextureSet>("HairTextureSet"));
                    },
                    x =>
                    {
                        x.EditorID = "Face";
                        x.Color.SetTo(FormKey.Factory("123456:badfacecolor.esp").AsLink<IColorRecordGetter>());
                    })
                .ToFormKeys()
                .ToList();
            var raceKey = groups
                .AddRecords<Race>(
                    "main.esp",
                    x =>
                    {
                        x.EditorID = "DefaultRace";
                        x.DefaultHairColors = new GenderedItem<IFormLinkGetter<IColorRecordGetter>>(
                            FormKey.Factory("123456:badmalehaircolor.esp").AsLink<IColorRecordGetter>(),
                            AddRecord<ColorRecord>("DefaultFemaleHairColor").AsLink<IColorRecordGetter>());
                        x.Skin = groups
                            .AddRecords<Armor>(
                                "main.esp",
                                a =>
                                {
                                    a.EditorID = "BodyArmor";
                                    a.Armature.AddRange(groups.AddRecords<ArmorAddon>(
                                        "main.esp",
                                        d =>
                                        {
                                            d.EditorID = "BodyAddon";
                                            d.AdditionalRaces.AddRange(AddRecords<Race>("AddRace1", "AddRace2"));
                                            d.AdditionalRaces.Add(FormKey.Factory("123456:badadditionalrace.esp"));
                                        },
                                        d =>
                                        {
                                            d.EditorID = "FeetAddon";
                                            var maleModel = new Model();
                                            var femaleModel = new Model
                                            {
                                                AlternateTextures = new(new[] {
                                                    new AlternateTexture
                                                    {
                                                        NewTexture = FormKey.Factory("123456:badfeettexture.esp")
                                                            .AsLink<ITextureSetGetter>(),
                                                    },
                                                    new AlternateTexture
                                                    {
                                                        NewTexture = AddRecord<TextureSet>("Feet2")
                                                            .AsLink<ITextureSetGetter>()
                                                    },
                                                }),
                                            };
                                            d.WorldModel = new GenderedItem<Model>(maleModel, femaleModel);
                                        }).ToFormKeys());
                                })
                            .Single()
                            .ToFormKey()
                            .AsNullableLink<IArmorGetter>();
                    })
                .Single()
                .ToFormKey();
            var npc = new Npc(FormKey.Factory("123456:main.esp"), SkyrimRelease.SkyrimSE)
            {
                EditorID = "TestNpc",
                Class = classKey.AsLink<IClassGetter>(),
                HeadParts = new(headPartKeys.Select(x => x.AsLink<IHeadPartGetter>())),
                Keywords = new(keywordKeys.Select(x => x.AsLink<IKeywordGetter>())),
                Race = raceKey.AsLink<IRaceGetter>()
            };
            var invalidPaths = checker.GetInvalidPaths(npc).ToList();

            Assert.Collection(
                invalidPaths,
                x => AssertPath(x, PathFrom(npc).Key("123456:badclass.esp", RecordType.Class)),
                x => AssertPath(x, PathFrom(npc).Key("123456:badkeyword.esp", RecordType.Keyword)),
                x => AssertPath(x, PathFrom(npc)
                    .Id("Hair")
                    .Id("Hairline")
                    .Key("123456:badhairlinetextureset.esp", RecordType.TextureSet)),
                x => AssertPath(x, PathFrom(npc).Id("Hair").Key("123456:badheadpart.esp", RecordType.HeadPart)),
                x => AssertPath(x, PathFrom(npc).Id("Face").Key("123456:badfacecolor.esp", RecordType.Color)),
                x => AssertPath(x, PathFrom(npc)
                    .Id("DefaultRace")
                    .Key("123456:badmalehaircolor.esp", RecordType.Color)),
                x => AssertPath(x, PathFrom(npc)
                    .Id("DefaultRace")
                    .Id("BodyArmor")
                    .Id("BodyAddon")
                    .Key("123456:badadditionalrace.esp", RecordType.Race)),
                x => AssertPath(x, PathFrom(npc)
                    .Id("DefaultRace")
                    .Id("BodyArmor")
                    .Id("FeetAddon")
                    .Key("123456:badfeettexture.esp", RecordType.TextureSet)));
        }

        [Fact]
        public void WhenPluginsExcluded_SkipsExcludedRecords()
        {
            var mainHeadPartKeys = groups
                .AddRecords<HeadPart>(
                    "main.esp",
                    x => x.EditorID = "Eyes",
                    x =>
                    {
                        x.EditorID = "Face";
                        x.ExtraParts.Add(FormKey.Factory("123456:badheadpart.esp").AsLink<IHeadPartGetter>());
                    })
                .ToFormKeys()
                .ToList();
            var excludedHeadPartKeys = groups
                .AddRecords<HeadPart>(
                    "excluded.esp",
                    x =>
                    {
                        x.EditorID = "Hair";
                        x.Color.SetTo(AddRecord<ColorRecord>("HairColor"));
                        // Adding these to main should still cause them to be ignored, because they are only reachable
                        // via the excluded plugin.
                        x.ExtraParts.AddRange(groups.AddRecords<HeadPart>("main.esp", hp =>
                        {
                            hp.EditorID = "Hairline";
                            hp.Color.SetTo(AddRecord<ColorRecord>("HairlineColor"));
                            hp.TextureSet.SetTo(FormKey.Factory("123456:badhairlinetextureset.esp"));
                        }).ToFormKeys());
                        x.TextureSet.SetTo(AddRecord<TextureSet>("HairTextureSet"));
                    })
                .ToFormKeys()
                .ToList();
            var headPartKeys = mainHeadPartKeys.Concat(excludedHeadPartKeys);
            var npc = new Npc(FormKey.Factory("123456:main.esp"), SkyrimRelease.SkyrimSE)
            {
                EditorID = "TestNpc",
                HeadParts = new(headPartKeys.Select(x => x.AsLink<IHeadPartGetter>())),
            };
            follower.WithPluginExclusions(new[] { "excluded.esp" });
            var invalidPaths = checker.GetInvalidPaths(npc).ToList();

            Assert.Collection(
                invalidPaths,
                x => AssertPath(x, PathFrom(npc).Id("Face").Key("123456:badheadpart.esp", RecordType.HeadPart)));
        }

        [Fact]
        public void IgnoresCircularReferences()
        {
            var headPartKeys = groups
                .AddRecords<HeadPart>(
                    "main.esp",
                    x =>
                    {
                        x.EditorID = "Hair";
                        x.Color.SetTo(AddRecord<ColorRecord>("HairColor"));
                        x.ExtraParts.AddRange(groups.AddRecords<HeadPart>("main.esp", hp =>
                        {
                            hp.EditorID = "Hairline";
                            hp.Color.SetTo(AddRecord<ColorRecord>("HairlineColor"));
                        }).ToFormKeys());
                        x.ExtraParts.Add(x.FormKey.AsLinkGetter<IHeadPartGetter>());
                        x.ExtraParts.Add(FormKey.Factory("123456:badheadpart.esp").AsLink<IHeadPartGetter>());
                        x.TextureSet.SetTo(AddRecord<TextureSet>("HairTextureSet"));
                    },
                    x =>
                    {
                        x.EditorID = "Face";
                        x.Color.SetTo(AddRecord<ColorRecord>("FaceColor"));
                    })
                .ToFormKeys();
            var npc = new Npc(FormKey.Factory("123456:main.esp"), SkyrimRelease.SkyrimSE)
            {
                EditorID = "TestNpc",
                HeadParts = new(headPartKeys.Select(x => x.AsLink<IHeadPartGetter>())),
            };
            var invalidPaths = checker.GetInvalidPaths(npc);

            Assert.Collection(
                invalidPaths,
                x => AssertPath(x, PathFrom(npc).Id("Hair").Key("123456:badheadpart.esp", RecordType.HeadPart)));
        }

        private FormKey AddRecord<T>(string editorId)
            where T : class, ISkyrimMajorRecord
        {
            return AddRecords<T>(editorId).Single();
        }

        private IEnumerable<FormKey> AddRecords<T>(params string[] editorIds)
            where T : class, ISkyrimMajorRecord
        {
            foreach (var editorId in editorIds)
                yield return groups.AddRecords<T>("main.esp", x => x.EditorID = editorId).Single().ToFormKey();
        }

        private void AssertPath(ReferencePath path, PathAssertionBuilder builder)
        {
            Assert.Collection(
                path.References,
                builder.References
                    .Select(x => (Action<ReferenceInfo>)(r =>
                    {
                        Assert.Equal(r.Key, x.Key);
                        Assert.Equal(r.EditorId, x.EditorId);
                        Assert.Equal(r.Type, x.Type);
                    }))
                    .ToArray());
        }

        private PathAssertionBuilder PathFrom<T>(T record)
            where T : ISkyrimMajorRecordGetter
        {
            var getterType = LoquiRegistration.GetRegister(typeof(T)).GetterType;
            return new PathAssertionBuilder(
                groups, new ReferenceInfo(record.FormKey.ToRecordKey(), getterType.GetRecordType(), record.EditorID));
        }

        class PathAssertionBuilder
        {
            public readonly List<ReferenceInfo> References = new();

            private readonly FakeGroupCache groups;

            public PathAssertionBuilder(FakeGroupCache groups, ReferenceInfo origin)
            {
                this.groups = groups;
                References.Add(origin);
            }

            public PathAssertionBuilder Id(string editorId)
            {
                var key = groups.FindKeyForEditorId(editorId, out var group);
                if (key == FormKey.Null)
                    throw new ArgumentException($"{editorId} not found in test cache");
                var record = group[key];
                var getterType = LoquiRegistration.GetRegister(record.GetType()).GetterType;
                References.Add(new ReferenceInfo(key.ToRecordKey(), getterType.GetRecordType(), editorId));
                return this;
            }

            public PathAssertionBuilder Key(string key, RecordType type)
            {
                References.Add(new ReferenceInfo(RecordKey.Parse(key), type));
                return this;
            }
        }
    }
}

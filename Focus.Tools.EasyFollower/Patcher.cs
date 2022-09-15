using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Focus.Tools.EasyFollower
{
    class Patcher
    {
        public record Result(
            string LocalFormIdHex,
            bool HasHairPhysics,
            IReadOnlySet<string> InvalidHeadPartNames);

        private const BipedObjectFlag WigFlags = BipedObjectFlag.Hair | BipedObjectFlag.LongHair;

        private static readonly Regex EditorIdRegex = new("[^A-Za-z0-9]", RegexOptions.Compiled);

        public NifEditor NifEditor => nifEditor;

        private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
        private readonly ILogger log;
        private readonly NifEditor nifEditor;

        public Patcher(
            IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env, NifEditor nifEditor, ILogger log)
        {
            this.env = env;
            this.log = log;
            this.nifEditor = nifEditor;
        }

        public bool Patch(
            string npcName, RaceMenuPreset preset, FollowerExportData exportData,
            string outputModName, OutputMode outputMode, [NotNullWhen(true)] out Result? result)
        {
            result = null;
            var modPath = Path.Combine(env.DataFolderPath, outputModName);
            var release = env.GameRelease.ToSkyrimRelease();
            bool isExistingMod = File.Exists(modPath);
            log.Information(
                isExistingMod
                    ? "Existing mod found at {modPath}, will attempt to update."
                    : "No mod found at {modPath}, a new mod will be created.",
                modPath);
            var mod = isExistingMod
                ? SkyrimMod.CreateFromBinary(modPath, release)
                : new SkyrimMod(outputModName, release);
            var npcEditorId = EditorIdRegex.Replace(npcName, "");
            bool isFemale = exportData.Sex == 1;
            var femaleFlag = isFemale ? NpcConfiguration.Flag.Female : 0;
            var race = GetFormLink<IRaceGetter>(exportData.Race);
            if (race == null)
            {
                log.Error("Invalid race {formId} found in export data", exportData.Race);
                return false;
            }
            var baseHeight = race.TryResolve(env.LinkCache)?.Height.PickGender(isFemale) ?? 1.0f;
            var relativeHeight = baseHeight != 0 ? exportData.Height / baseHeight : 1.0f;
            var hairColor = AddHairColor(mod, npcEditorId, preset.Actor.HairColor);
            var defaultOutfit = AddOutfit(mod, npcEditorId, exportData.Equipment);
            var outfitItemKeys = defaultOutfit?.Items?.Select(x => x.FormKey).ToHashSet() ?? new();
            var headPartIdentifiers = preset.HeadParts.Select(x => x.FormIdentifier);
            var hairParts = GetHairParts(headPartIdentifiers).ToList();
            var wornArmor = MaybeAddWornArmor(
                mod, npcEditorId, race.TryResolve(env.LinkCache), isFemale, hairParts, outputMode);
            var headParts = GetFormLinks<IHeadPartGetter>(headPartIdentifiers) ?? new();
            if (wornArmor != null)
            {
                log.Debug("Removing normal hair parts due to wig requirement.");
                headParts.RemoveAll(x => hairParts.Any(p => p.FormKey == x.FormKey));
                var baldHair = AddBaldHeadPart(mod);
                headParts.Add(baldHair.ToLinkGetter());
            }
            var npc = new Npc(mod, npcEditorId)
            {
                Name = npcName,
                // Common bounds used for all human NPCs.
                ObjectBounds = new ObjectBounds
                {
                    First = new P3Int16(-22, -14, 0),
                    Second = new P3Int16(22, 14, 128),
                },
                Configuration = new NpcConfiguration
                {
                    Flags =
                        NpcConfiguration.Flag.Essential |
                        NpcConfiguration.Flag.Unique |
                        NpcConfiguration.Flag.AutoCalcStats |
                        femaleFlag,
                    DispositionBase = 35, // Not used, but this is the default
                    Level = new PcLevelMult
                    {
                        LevelMult = 1,
                    },
                    CalcMinLevel = 5,
                    CalcMaxLevel = 100,
                    SpeedMultiplier = 100,
                },
                Race = race,
                AttackRace = race.AsNullable(),
                Height = relativeHeight,
                Weight = preset.Actor.Weight,
                HeadParts = headParts,
                HairColor = hairColor.ToNullableLink(),
                HeadTexture = GetNullableFormLink<ITextureSetGetter>(preset.Actor.HeadTexture),
                TextureLighting = exportData.SkinToneColor != 0
                    ? Color.FromArgb(exportData.SkinToneColor) : Color.White,
                // We don't really need the face morph, but it's essentially free.
                FaceMorph = ConvertFaceMorph(preset.Morphs.Default.Morphs),
                WornArmor = wornArmor?.ToNullableLink() ?? new FormLinkNullable<Armor>(),
                DefaultOutfit =
                    defaultOutfit?.ToNullableLink<IOutfitGetter>()
                    ?? new FormLinkNullable<IOutfitGetter>(),
                Voice = isFemale
                    ? FormLinks.FemaleCommonerVoiceType.AsNullable()
                    : FormLinks.MaleCommonerVoiceType.AsNullable(),
                SoundLevel = SoundLevel.Normal,
                AIData = new AIData
                {
                    Aggression = Aggression.Agressive,
                    Confidence = Confidence.Brave,
                    EnergyLevel = 50, // Creation Kit default
                    Mood = Mood.Neutral,
                    Responsibility = Responsibility.AnyCrime,
                    Assistance = Assistance.HelpsFriendsAndAllies,
                },
                Class = FormLinks.CombatWarrior1H,
                Factions = new ExtendedList<RankPlacement>
                {
                    new RankPlacement
                    {
                        Faction = FormLinks.PotentialMarriageFaction,
                        Rank = 0,
                    },
                    new RankPlacement
                    {
                        Faction = FormLinks.PotentialFollowerFaction,
                        Rank = 0,
                    },
                    new RankPlacement
                    {
                        Faction = FormLinks.CurrentFollowerFaction,
                        Rank = unchecked((byte)-1),
                    },
                },
                ActorEffect =
                    GetFormLinks<ISpellRecordGetter>(
                        exportData.Abilities.Concat(exportData.Spells),
                        keyBlacklist: FormLinks.PCHealRateCombat)
                    ?? new(),
                Packages = new ExtendedList<IFormLinkGetter<IPackageGetter>>
                {
                    FormLinks.DefaultSandboxEditorLocation512,
                },
                Items = exportData.Inventory
                    .Select(x => new
                    {
                        FormLink = GetFormLink<IItemGetter>(x.FormIdentifier),
                        x.Count,
                    })
                    .Where(x => x.FormLink != null && !outfitItemKeys.Contains(x.FormLink.FormKey))
                    .Select(x => new ContainerEntry
                    {
                        Item = new ContainerItem
                        {
                            Item = x.FormLink!,
                            Count = x.Count,
                        }
                    })
                    .ToExtendedList(),
                PlayerSkills = new PlayerSkills
                {
                    Health = 100,
                    Magicka = 50,
                    Stamina = 60,
                    SkillValues = {
                        // These seem to be relatively common values for CombatWarrior1H.
                        // Anyway, we only add them so that the NPC has something sensible to start
                        // with; the assumption is they'll be edited later.
                        { Skill.OneHanded, 27 },
                        { Skill.TwoHanded, 27 },
                        { Skill.Archery, 19 },
                        { Skill.Block, 24 },
                        { Skill.Smithing, 20 },
                        { Skill.HeavyArmor, 22 },
                        { Skill.LightArmor, 20 },
                        { Skill.Pickpocket, 25 },
                        { Skill.Lockpicking, 15 },
                        { Skill.Sneak, 15 },
                        { Skill.Alchemy, 15 },
                        { Skill.Speech, 20 },
                        { Skill.Alteration, 15 },
                        { Skill.Conjuration, 15 },
                        { Skill.Destruction, 15 },
                        { Skill.Illusion, 15 },
                        { Skill.Restoration, 15 },
                        { Skill.Enchanting, 15 },
                    },
                }
                // Face parts and tints are ignored, since they're hard to do properly and we don't
                // need them anyway (appearance comes from the exported facegen nif/dds files).
            };
            AddOverlayScript(npc, preset.Overrides);
            mod.Npcs.ReplaceByEditorId(ref npc);
            AddRelationship(mod, npc, Relationship.RankType.Ally);
            if (outputMode == OutputMode.NormalWithBackup)
                Files.Backup(modPath);
            if (outputMode != OutputMode.DryRun)
            {
                mod.WriteToBinary(modPath, new BinaryWriteParameters
                {
                    MastersListOrdering = new MastersListOrderingByLoadOrder(env.LoadOrder),
                });
                log.Information("Wrote NPC records to {modPath}", modPath);
            }
            else
                log.Information("Skipped writing NPC records to {modPath} due to dry-run", modPath);
            var localFormIdHex = npc.FormKey.IDString();
            var invalidHeadPartNames = wornArmor != null
                ? hairParts.Select(x => x.EditorID).NotNullOrEmpty()
                : Enumerable.Empty<string>();
            result = new(localFormIdHex, wornArmor != null, invalidHeadPartNames.ToHashSet());
            return true;
        }

        private HeadPart AddBaldHeadPart(ISkyrimMod mod)
        {
            HeadPart headPart = new HeadPart(mod, "EasyFollowerBaldHair")
            {
                MajorFlags = HeadPart.MajorFlag.NonPlayable,
                Flags = HeadPart.Flag.Male | HeadPart.Flag.Female,
                Type = HeadPart.TypeEnum.Hair,
                ValidRaces = FormLinks.HeadPartsAllRacesMinusBeast.AsNullable(),
            };
            mod.HeadParts.ReplaceByEditorId(ref headPart);
            return headPart;
        }

        private ColorRecord AddHairColor(ISkyrimMod mod, string npcEditorId, int colorArgb)
        {
            ColorRecord hairColor = new ColorRecord(mod, $"{npcEditorId}HairColor")
            {
                Color = Color.FromArgb(colorArgb),
                Playable = false,
            };
            mod.Colors.ReplaceByEditorId(ref hairColor);
            return hairColor;
        }

        private void AddOverlayScript(Npc npc, IEnumerable<Override> overrides)
        {
            log.Debug("Checking NiOverride values...");
            var overrideSettings = NiNodeOverrideSettings.FromRaceMenuPreset(overrides).ToList();
            if (overrideSettings.Count == 0)
            {
                log.Information("No NI overrides found.");
                return;
            }
            log.Information("Found {overrideCount} NI node overrides", overrideSettings.Count);
            npc.VirtualMachineAdapter = new VirtualMachineAdapter
            {
                Scripts = new ExtendedList<ScriptEntry>
                {
                    new ScriptEntry
                    {
                        Name = "FTEasyFollowerOverlayScript",
                        Properties = new ExtendedList<ScriptProperty>
                        {
                            new ScriptStringListProperty
                            {
                                Name = "NodeNames",
                                Data = new(overrideSettings.Select(x => x.NodeName)),
                            },
                            new ScriptIntListProperty
                            {
                                Name = "NodeIndices",
                                Data = new(overrideSettings.Select(x => x.TextureIndex)),
                            },
                            new ScriptStringListProperty
                            {
                                Name = "TexturePaths",
                                Data = new(overrideSettings.Select(x => x.TexturePath)),
                            },
                            new ScriptIntListProperty
                            {
                                Name = "TintColors",
                                Data = new(overrideSettings.Select(x => x.TintColor.ToArgb())),
                            },
                            new ScriptIntListProperty
                            {
                                Name = "EmissiveColors",
                                Data = new(overrideSettings.Select(x => x.EmissiveColor.ToArgb())),
                            },
                        },
                    }
                }
            };
        }

        private Outfit? AddOutfit(ISkyrimMod mod, string npcEditorId, IEnumerable<string> equipment)
        {
            if (!equipment.Any())
                return null;
            var outfit = new Outfit(mod, $"{npcEditorId}Outfit")
            {
                Items = GetFormLinks<IOutfitTargetGetter>(equipment),
            };
            mod.Outfits.ReplaceByEditorId(ref outfit);
            return outfit;
        }

        private Relationship AddRelationship(
            ISkyrimMod mod, INpcGetter npc, Relationship.RankType rank)
        {
            Relationship relationship = new Relationship(mod, $"{npc.EditorID}Relationship")
            {
                Parent = npc.ToLink(),
                Child = FormLinks.Player,
                Rank = rank,
            };
            mod.Relationships.ReplaceByEditorId(ref relationship);
            return relationship;
        }

        private static NpcFaceMorph ConvertFaceMorph(IList<float> morphValues)
        {
            return new NpcFaceMorph
            {
                NoseLongVsShort = morphValues.GetOrDefault(0),
                NoseUpVsDown = morphValues.GetOrDefault(1),
                JawUpVsDown = morphValues.GetOrDefault(2),
                JawNarrowVsWide = morphValues.GetOrDefault(3),
                JawForwardVsBack = morphValues.GetOrDefault(4),
                CheeksUpVsDown = morphValues.GetOrDefault(5),
                CheeksForwardVsBack = morphValues.GetOrDefault(6),
                EyesUpVsDown = morphValues.GetOrDefault(7),
                EyesInVsOut = morphValues.GetOrDefault(8),
                BrowsUpVsDown = morphValues.GetOrDefault(9),
                BrowsInVsOut = morphValues.GetOrDefault(10),
                BrowsForwardVsBack = morphValues.GetOrDefault(11),
                LipsUpVsDown = morphValues.GetOrDefault(12),
                LipsInVsOut = morphValues.GetOrDefault(13),
                ChinNarrowVsWide = morphValues.GetOrDefault(14),
                ChinUpVsDown = morphValues.GetOrDefault(15),
                ChinUnderbiteVsOverbite = morphValues.GetOrDefault(16),
                EyesForwardVsBack = morphValues.GetOrDefault(17),
                Unknown = morphValues.GetOrDefault(18),
            };
        }

        private IFormLink<T>? GetFormLink<T>(string formIdFromExportData)
            where T : class, IMajorRecordGetter
        {
            // Internally we (and Mutagen) use colon-separated, but the script uses pipe-separated
            // in order to be more consistent with RaceMenu.
            var tokens = formIdFromExportData.Split(new[] { '|' });
            if (tokens.Length != 2)
                return null;
            var formKey = new RecordKey(tokens[0], tokens[1]).ToFormKey();
            // Our own scripts will recognize ESLs and emit the correct form IDs, but others (e.g.
            // CharGen) will won't. A hack we can use is to bitmask the ID here as well, regardless
            // of what the data says, if we know that the plugin is in fact Light. There's no 100%
            // guarantee that it will be correct, but it's 100% incorrect to reference any form IDs
            // larger than 12 bits in an ESL.
            if (IsLightMaster(formKey.ModKey))
                formKey = new FormKey(formKey.ModKey, formKey.ID & 0x000fff);
            return formKey.ToLink<T>();
        }

        private ExtendedList<IFormLinkGetter<T>>? GetFormLinks<T>(
            IEnumerable<string> formIdsFromExportData, params IFormLinkGetter<T>[] keyBlacklist)
            where T : class, IMajorRecordGetter
        {
            if (formIdsFromExportData == null || !formIdsFromExportData.Any())
                return null;
            var links = formIdsFromExportData
                .Select(x => GetFormLink<T>(x))
                .NotNull()
                .Where(x => !keyBlacklist.Contains(x.FormKey))
                .ToList();
            return links.Count > 0 ? new ExtendedList<IFormLinkGetter<T>>(links) : null;
        }

        private IEnumerable<IHeadPartGetter> GetHairParts(IEnumerable<string> headPartIdentifiers)
        {
            var hairParts = headPartIdentifiers
                .Select(id => GetFormLink<IHeadPartGetter>(id))
                .NotNull()
                .Select(x => x.TryResolve(env.LinkCache))
                .NotNull()
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .ToList();
            var parentParts = hairParts;
            do
            {
                var extraParts = parentParts
                    .SelectMany(x => x.ExtraParts)
                    .Select(x => x.TryResolve(env.LinkCache))
                    .NotNull()
                    .ToList();
                hairParts.AddRange(extraParts);
                parentParts = extraParts;
            } while (parentParts.Any());
            return hairParts;
        }

        private IFormLinkNullable<T> GetNullableFormLink<T>(string formIdFromExportData)
            where T : class, IMajorRecordGetter
        {
            var link = GetFormLink<T>(formIdFromExportData);
            return link?.AsNullable() ?? new FormLinkNullable<T>();
        }

        private bool IsLightMaster(ModKey modKey)
        {
            if (modKey.FileName.Extension.Equals(".esl", StringComparison.OrdinalIgnoreCase))
                return true;
            return env.LoadOrder.TryGetValue(modKey, out var listing) &&
                (listing.Mod!.ModHeader.Flags & SkyrimModHeader.HeaderFlag.LightMaster) != 0;
        }

        private Armor? MaybeAddWornArmor(
            ISkyrimMod mod, string npcEditorId, IRaceGetter? race, bool isFemale,
            IEnumerable<IHeadPartGetter> hairParts, OutputMode outputMode)
        {
            log.Debug("Checking for hair physics");
            var hairModels = hairParts
                .Select(x => x.Model?.File)
                .NotNullOrEmpty()
                .Select(x => x.PrefixPath("meshes"))
                .ToList();
            var hasPhysics = hairModels.Any(f => nifEditor.HasHdtPhysics(f));
            if (!hasPhysics)
            {
                log.Information("No hair physics found; wig not required.");
                return null;
            }

            log.Information("Hair physics detected; a wig (WNAM) will be created.");
            var wigRelativePath = $@"Armor\EasyFollowerWigs\{npcEditorId}.nif";
            var wigPath = Path.Combine(env.DataFolderPath, "meshes", wigRelativePath);
            nifEditor.MergeModels(hairModels, wigPath, outputMode);
            const string unusedMaleModelFile = @"Armor\Hide\M\HelmetLight.nif";
            const string unusedFemaleModelFile = @"Armor\Hide\F\HelmetLight.nif";
            var defaultSkin = race?.Skin.TryResolve<IArmorGetter>(env.LinkCache);
            var defaultSkinAddons = defaultSkin != null
                ? defaultSkin.Armature
                    .Select(x => x.TryResolve<IArmorAddonGetter>(env.LinkCache))
                    .NotNull()
                    .Where(x => ((x.BodyTemplate?.FirstPersonFlags ?? 0) & WigFlags) == 0)
                    .Select(x => x.ToLinkGetter())
                : new[] { FormLinks.NakedTorso, FormLinks.NakedHands, FormLinks.NakedFeet };
            var additionalRaces = defaultSkinAddons
                .Select(x => x.TryResolve(env.LinkCache))
                .NotNull()
                .SelectMany(x => x.AdditionalRaces)
                .DistinctBy(x => x.FormKey);
            var wigAddon = new ArmorAddon(mod, $"{npcEditorId}WigAddon")
            {
                BodyTemplate = new BodyTemplate
                {
                    ArmorType = ArmorType.Clothing,
                    FirstPersonFlags = WigFlags,
                },
                Race = FormLinks.DefaultRace.AsNullable(),
                Priority = new GenderedItem<byte>(10, 10),
                WeaponAdjust = -4,
                WorldModel = new GenderedItem<Model?>(
                    // Male
                    new Model { File = isFemale ? unusedMaleModelFile : wigRelativePath },
                    // Female
                    new Model
                    { File = isFemale ? wigRelativePath : unusedFemaleModelFile }),
                AdditionalRaces = new(additionalRaces),
            };
            mod.ArmorAddons.ReplaceByEditorId(ref wigAddon);
            var defaultSkinFlags =
                defaultSkin?.BodyTemplate?.FirstPersonFlags
                ?? BipedObjectFlag.Head | BipedObjectFlag.Body |
                BipedObjectFlag.Hands | BipedObjectFlag.Feet;
            var wornArmor = new Armor(mod, $"{npcEditorId}Skin")
            {
                MajorFlags = Armor.MajorFlag.NonPlayable,
                BodyTemplate = new BodyTemplate
                {
                    FirstPersonFlags = defaultSkinFlags | WigFlags,
                    ArmorType = ArmorType.Clothing,
                },
                Race = race?.ToNullableLink() ?? new FormLinkNullable<IRaceGetter>(),
                Armature = new(defaultSkinAddons.Append(wigAddon.ToLinkGetter())),
            };
            mod.Armors.ReplaceByEditorId(ref wornArmor);
            return wornArmor;
        }
    }
}

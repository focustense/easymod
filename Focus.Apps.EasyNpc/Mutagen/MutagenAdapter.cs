﻿#nullable enable

using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Compatibility;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using Focus.Apps.EasyNpc.Main;
using Focus.Environment;
using Focus.Files;
using Focus.ModManagers;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Installs;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NpcFaceParts = Focus.Apps.EasyNpc.GameData.Records.NpcFaceParts;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenAdapter : IGameDataEditor<FormKey>
    {
        public IArchiveProvider ArchiveProvider { get; private set; }
        public GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> Environment { get; private set; }
        public string DataDirectory { get; private set; }
        // Mutagen doesn't have an internal log, like XEdit Lib. (Because it doesn't need to, as it's a .NET library and
        // works with ordinary exception handling)
        public IExternalLog Log { get; init; } = new NullExternalLog();
        public IMergedPluginBuilder<FormKey> MergedPluginBuilder { get; private set; }
        public IEnumerable<ISkyrimModGetter> Mods => Environment.LoadOrder.Select(x => x.Value.Mod).NotNull();
        public IModPluginMapFactory ModPluginMapFactory { get; private set; }

        private readonly GameRelease gameRelease;
        private readonly ILogger log;
        private readonly IModResolver modResolver;
        private readonly SkyrimRelease skyrimRelease;

        private CompatibilityRuleSet<INpcGetter> npcCompatibilityRuleSet;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MutagenAdapter(string gameName, string gamePath, IModResolver modResolver, ILogger log)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            var isValidGameName = Enum.TryParse<GameRelease>(gameName, true, out var gameRelease);
            if (!isValidGameName || !Enum.TryParse<SkyrimRelease>(gameName, true, out var skyrimRelease))
                throw new UnsupportedGameException(gameName, isValidGameName ? GetGameName(gameRelease) : null);
            this.gameRelease = gameRelease;
            this.skyrimRelease = skyrimRelease;
            if (!string.IsNullOrEmpty(gamePath))
                DataDirectory = gamePath;
            else
            {
                if (!GameLocations.TryGetDataFolder(gameRelease, out var dataFolder))
                    throw new MissingGameDataException(Enum.GetName(gameRelease)!, GetGameName(gameRelease));
                DataDirectory = dataFolder;
            }
            this.log = log;
            this.modResolver = modResolver;
        }

        public IEnumerable<PluginInfo> GetAvailablePlugins()
        {
            var implicits = Implicits.BaseMasters.Skyrim(skyrimRelease)
                .Select(x => x.FileName.String)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return LoadOrder.GetListings(gameRelease, DataDirectory, true)
                .Select((x, i) =>
                {
                    var isReadable = TryGetMasterNames(x.ModKey.FileName, out var masterNames);
                    return new PluginInfo
                    {
                        FileName = x.ModKey.FileName.String,
                        IsEnabled = x.Enabled,
                        IsImplicit = implicits.Contains(x.ModKey.FileName.String),
                        IsReadable = isReadable,
                        Masters = masterNames.ToList().AsReadOnly(),
                    };
                })
                .Where(x => x is not null);
        }

        public IEnumerable<string> GetLoadedPlugins()
        {
            return Environment.LoadOrder.Select(x => x.Key.FileName.String);
        }

        public int GetLoadOrderIndex(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            return Environment.LoadOrder.IndexOf(modKey);
        }

        public bool IsMaster(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var listing = Environment.LoadOrder.TryGetValue(modKey);
            return listing is not null && listing.Mod is not null &&
                listing.Mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Master);
        }

        public Task Load(IEnumerable<string> pluginNames)
        {
            return Task.Run(() =>
            {
                var loadOrderKeys = pluginNames.Select(pluginName => ModKey.FromNameAndExtension(pluginName));
                var loadOrder = LoadOrder.Import<ISkyrimModGetter>(DataDirectory, loadOrderKeys, gameRelease);
                var linkCache = loadOrder.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
                // If we actually managed to get here, then earlier code already managed to find the listings file.
                var listingsFile = PluginListings.GetListingsFile(gameRelease);
                var creationClubFile = CreationClubListings.GetListingsPath(gameRelease.ToCategory(), DataDirectory);
                Environment = new GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>(
                    DataDirectory, listingsFile, creationClubFile, loadOrder, linkCache, true);
                Environment.LinkCache.Warmup<Npc>();
                ArchiveProvider = new MutagenArchiveProvider(Environment, gameRelease, log);
                MergedPluginBuilder = new MutagenMergedPluginBuilder(Environment, skyrimRelease, log);
                ModPluginMapFactory = new MutagenModPluginMapFactory(Environment, gameRelease, modResolver);
                npcCompatibilityRuleSet = new CompatibilityRuleSet<INpcGetter>(npc => $"{npc.FormKey} '{npc.EditorID}'", log)
                    .Add(new FacegenHeadRule(Environment))
                    .Add(new NoChildrenRule(Environment));
                npcCompatibilityRuleSet.ReportConfiguration();
            });
        }

        public IEnumerable<Hair<FormKey>> ReadHairRecords(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var listing = Environment.LoadOrder.GetIfEnabled(modKey);
            return listing?.Mod?.HeadParts
                ?.Where(x => x.Type == HeadPart.TypeEnum.Hair && !x.Flags.HasFlag(HeadPart.Flag.IsExtraPart))
                ?.Select(x => new Hair<FormKey>
                {
                    Key = x.FormKey,
                    EditorId = x.EditorID,
                    Name = x.Name?.ToString(),
                    ModelFileName = x.Model?.File,
                    IsFemale = x.Flags.HasFlag(HeadPart.Flag.Female),
                    IsMale = x.Flags.HasFlag(HeadPart.Flag.Male),
                    ValidRaces = GetValidRaces(x).ToHashSet(),
                }) ?? Enumerable.Empty<Hair<FormKey>>();
        }

        public void ReadNpcRecords(string pluginName, IDictionary<FormKey, IMutableNpc<FormKey>> cache)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var mod = GetMod(modKey);
            if (mod is null)
                return;

            log.Debug("Checking master references");
            var masters = mod.ModHeader.MasterReferences.Select(x => x.Master).ToHashSet();
            var npcContexts = mod.EnumerateMajorRecordContexts<INpc, INpcGetter>(Environment.LinkCache);
            foreach (var npcContext in npcContexts)
            {
                var formKey = npcContext.Record.FormKey;
                log.Debug($"Processing {formKey} '{npcContext.Record.EditorID}'");
                if (formKey.ModKey != modKey)
                {
                    log.Debug("Record is an override, starting analysis");
                    if (!cache.TryGetValue(formKey, out var npc))
                    {
                        var declaringMod = GetMod(formKey.ModKey);
                        if (declaringMod is null)
                        {
                            log.Fatal(
                                $"Plugin [{mod.ModKey}] requires master [{formKey.ModKey}], but this master is " +
                                $"either not loaded or is incorrectly configured to load after [{mod.ModKey}].");
                            throw new Exception($"NPC [{formKey}] references missing master [{formKey.ModKey}]");
                        }
                        if (!declaringMod.Npcs.ContainsKey(formKey))
                        {
                            log.Warning(
                                $"Plugin [{mod.ModKey}] references invalid or 'injected' (unsupported) NPC " +
                                $"[{formKey}] ('{npcContext.Record.EditorID}'). This NPC will be ignored.");
                            continue;
                        }
                    }
                    var comparison = GetComparisonRecord(npcContext, out var itpoPluginName);
                    log.Debug("Checking for face edits");
                    var faceData = GetFaceOverrides(npcContext.Record, comparison, out bool affectsFaceGen);
                    var npcOverride = new NpcOverride<FormKey>(modKey.FileName)
                    {
                        FaceData = faceData,
                        FaceOverridesAffectFaceGen = affectsFaceGen,
                        ItpoPluginName = itpoPluginName,
                        ModifiesBehavior = ModifiesBehavior(npcContext, masters),
                        ModifiesBody = ModifiesBody(npcContext.Record, comparison),
                        ModifiesOutfits = ModifiesOutfits(npcContext.Record, comparison),
                        Race = npcContext.Record.Race.FormKeyNullable?.ToRecordKey(),
                        Wig = GetWigInfo(npcContext.Record),
                    };
                    log.Debug("Completed checks for NPC override");
                    npc!.AddOverride(npcOverride);
                }
                else
                {
                    log.Debug("Record is the master");
                    cache.Add(formKey, new NpcInfo<FormKey>
                    {
                        BasePluginName = modKey.FileName,
                        DefaultRace = npcContext.Record.Race.FormKeyNullable?.ToRecordKey(),
                        EditorId = npcContext.Record.EditorID,
                        IsFemale = npcContext.Record.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female),
                        Key = formKey,
                        IsSupported = npcCompatibilityRuleSet.IsSupported(npcContext.Record),
                        LocalFormIdHex = formKey.ID.ToString("X6"),
                        Name = npcContext.Record.Name?.ToString(),
                    });
                }
            }
        }

        private INpcGetter? GetComparisonRecord(IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> npcContext,
            out string? itpoPluginName)
        {
            itpoPluginName = null;
            var previousOverride = GetPreviousOverride(npcContext);
            if (previousOverride is null)   // We were already on the master
                return null;
            var isItpo = NpcsSame(npcContext.Record, previousOverride.Record);
            if (isItpo)
                itpoPluginName = previousOverride.ModKey.FileName;
            // This logic may appear strange - if it is not identical to the previous override, then why go directly to
            // the master and ignore all overrides in between?
            // What it boils down to is inferred intent. What we really care about for comparison purposes is whether or
            // not it changes attributes from the master - the true master, that originally declared the record.
            // However, ITPOs in particular have no functional effect on the NPC in the current load order, and so we
            // don't want to end up picking them as defaults. If it's not strictly ITPO, but happens to be identical to
            // some other override elsewhere in the load order, then that doesn't matter - it still changes the NPC.
            return isItpo ? Environment.LoadOrder.GetMasterNpc(npcContext.Record.FormKey) : previousOverride.Record;
        }

        private NpcFaceData<FormKey>? GetFaceOverrides(INpcGetter npc, INpcGetter? comparison, out bool affectsFaceGen)
        {
            affectsFaceGen = false;
            if (comparison is null)
                return null;

            var overrideFaceData = ReadFaceData(npc);
            var previousFaceData = ReadFaceData(comparison);
            var overrideRace = npc.Race.FormKeyNullable;
            var previousRace = comparison.Race.FormKeyNullable;
            affectsFaceGen = !NpcFaceData.EqualsForFaceGen(overrideFaceData, previousFaceData);
            return (!NpcFaceData.Equals(overrideFaceData, previousFaceData) || (overrideRace != previousRace)) ?
                overrideFaceData : null;
        }

        private static string GetGameName(GameRelease gameRelease) => gameRelease switch
        {
            GameRelease.EnderalLE => "Enderal Legendary Edition",
            GameRelease.EnderalSE => "Enderal Special Edition",
            GameRelease.Fallout4 => "Fallout 4",
            GameRelease.Oblivion => "Oblivion",
            GameRelease.SkyrimLE => "Skyrim Legendary Edition",
            GameRelease.SkyrimSE => "Skyrim Special Edition",
            GameRelease.SkyrimVR => "Skyrim VR",
            _ => "Unknown game"
        };

        private ISkyrimModGetter? GetMod(ModKey key)
        {
            var listing = Environment.LoadOrder.GetIfEnabled(key);
            return listing?.Mod;
        }

        private IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>? GetPreviousOverride(
            IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> npcContext)
        {
            var formLink = npcContext.Record.FormKey.AsLink<INpcGetter>();
            return formLink
                .ResolveAllContexts<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>(Environment.LinkCache)
                .SkipWhile(x => x.ModKey != npcContext.ModKey)
                .Skip(1)
                .FirstOrDefault();
        }

        private IEnumerable<VanillaRace> GetValidRaces(IHeadPartGetter headPart)
        {
            if (headPart.ValidRaces.IsNull)
                return Enumerable.Empty<VanillaRace>();
            var raceList = headPart.ValidRaces.FormKey.AsLink<IFormListGetter>()
                .TryResolve(Environment.LinkCache, headPart, log, "Hair/wig conversion will be unavailable.");
            if (raceList == null)
                return Enumerable.Empty<VanillaRace>();
            return raceList.Items
                .Select(x => x.FormKey.AsLink<IRaceGetter>()
                    .TryResolve(Environment.LinkCache, raceList, log, "Hair/wig conversion may not work correctly."))
                .NotNull()
                .Select(x => InferRace(x.EditorID));
        }

        private NpcWigInfo<FormKey>? GetWigInfo(INpcGetter npc)
        {
            log.Debug("Checking for wigs");
            if (npc.WornArmor.IsNull)
                return null;
            var isBald = npc.HeadParts
                .Select(x => x.FormKey.AsLink<IHeadPartGetter>().TryResolve(Environment.LinkCache))
                .NotNull()
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .All(x => string.IsNullOrEmpty(x.Model?.File));
            var wornArmor =
                npc.WornArmor.TryResolve(Environment.LinkCache, npc, log,
                "Wig and body customizations for this NPC will be ignored.");
            if (wornArmor == null)
                return null;
            return wornArmor.Armature
                .Select(fk => fk.TryResolve(
                    Environment.LinkCache, wornArmor, log, "Wigs may not be correctly detected."))
                .NotNull()
                .Where(x =>
                    // Search for ONLY hair, because some sadistic modders add hair flags to other parts.
                    x.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.Hair ||
                    x.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.LongHair ||
                    x.BodyTemplate?.FirstPersonFlags == (BipedObjectFlag.Hair | BipedObjectFlag.LongHair))
                .Select(x => {
                    var modelFileName = x.WorldModel?.Where(x => x is not null)?.FirstOrDefault()?.File;
                    var modelName = !string.IsNullOrEmpty(modelFileName) ?
                        Path.GetFileNameWithoutExtension(modelFileName) : null;
                    return new NpcWigInfo<FormKey>(x.FormKey, modelName, isBald);
                })
                .FirstOrDefault();
        }

        private static VanillaRace InferRace(string? editorId)
        {
            return editorId switch
            {
                "NordRace" => VanillaRace.Nord,
                "ImperialRace" => VanillaRace.Imperial,
                "RedguardRace" => VanillaRace.Redguard,
                "BretonRace" => VanillaRace.Breton,
                "HighElfRace" => VanillaRace.HighElf,
                "DarkElfRace" => VanillaRace.DarkElf,
                "WoodElfRace" => VanillaRace.WoodElf,
                "OrcRace" => VanillaRace.Orc,
                "ElderRace" => VanillaRace.Elder,
                _ => 0,
            };
        }

        private bool ModifiesBehavior(
            IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> npcContext, IReadOnlySet<ModKey> masterKeys)
        {
            log.Debug("Checking for behavior edits");
            // The rules for behavior need to be a little different from those for the face, because the semi-common
            // practice is for NPC overhauls to inherit from "foundation" mods like USSEP.
            // Bethesda's multiple-master system is opaque and doesn't tell us anything about precisely *how* a master
            // is being used, so for the time being, the solution is something equally blunt:
            // Check to see if the behavior of this NPC is identical to the behavior of ANY of the plugin's masters. If
            // so, it "probably" does not intend to be a behavior mod, and is just trying to avoid breaking other mods
            // that do make intentional changes.
            // This, of course, will not work at all if the extra master (e.g. USSEP) is changed and the plugin is not
            // updated. Which is why mod creators really should distribute compatibility patches as separate files,
            // despite the fact that some don't.
            var previousOverrides = npcContext.Record.FormKey.AsLink<INpcGetter>()
                .ResolveAllContexts<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>(Environment.LinkCache)
                .SkipWhile(x => x.ModKey != npcContext.ModKey)
                .Skip(1)
                .Where(x => masterKeys.Contains(x.ModKey));
            return previousOverrides.All(x => ModifiesBehavior(npcContext.Record, x.Record));
        }

        private static bool ModifiesBehavior(INpcGetter current, INpcGetter previous)
        {
            if (previous is null)
                return false;

            // For the time being, we can interpret "modifying behavior" as modifying anything that is _not_ the face.
            //
            // This is somewhat of a shotgun solution, but more precise than looking for non-ITPO records that _don't_
            // modify the face, as some mostly-behavior plugins do make edits to the face - possibly accidentally.
            var sameExcludingConfigurationFlags = NpcsSame(current, previous, mask =>
            {
                mask.Configuration = false;
                // Outfits may be considered part of "behavior" for merging, but they are checked separately.
                mask.DefaultOutfit = false;
                mask.FaceMorph = false;
                mask.FaceParts = false;
                mask.HairColor = false;
                mask.HeadParts = false;
                mask.HeadTexture = false;
                mask.SleepingOutfit = false;
                mask.TextureLighting = false;
                mask.TintLayers = false;
                mask.Height = false;
                mask.Weight = false;
                // Bodies behave similarly to outfits - not considered "behavior" unless isolated.
                mask.WornArmor = false;
            });
            return !sameExcludingConfigurationFlags ||
                // We apparently have to compare the configurations separately, because Mutagen doesn't handle it right.
                // Literally, the configurations will compare equal with the mask, while the NPCs will compare unequal
                // using a default-empty (false, false) mask including the same configuration mask.
                !current.Configuration.Equals(previous.Configuration, new NpcConfiguration.TranslationMask(true, true)
                {
                    Flags = false,
                }) ||
                // Overhauls might modify the Opposite Gender Animations flag. This is considered a visual change, not
                // a behavior change. If a mod ONLY modifies this flag and nothing else about an NPC, it might be
                // ignored. There were some very old mods that did this, but they're not so common anymore, and if they
                // do exist, they probably conflict with USSEP etc.
                (current.Configuration.Flags & ~NpcConfiguration.Flag.OppositeGenderAnims) !=
                    (previous.Configuration.Flags & ~NpcConfiguration.Flag.OppositeGenderAnims);
        }

        private bool ModifiesBody(INpcGetter current, INpcGetter? previous)
        {
            log.Debug("Checking for body edits");
            return previous is not null && current.WornArmor == previous.WornArmor;
        }

        private bool ModifiesOutfits(INpcGetter current, INpcGetter? previous)
        {
            log.Debug("Checking for outfit edits");
            if (previous is null)
                return false;

            // Outfits are currently a tossup in terms of whether they should be treated with the same logic as face
            // overrides (only check PO or Declaring Master), or behavior overrides (check all masters), especially
            // we don't actually handle outfit carry-over yet, only using it as a signal for other checks.
            // This may change once outfits are actually implemented.
            return current.DefaultOutfit == previous.DefaultOutfit && current.SleepingOutfit == previous.SleepingOutfit;
        }

        private static bool NpcsSame(INpcGetter x, INpcGetter y, Action<Npc.TranslationMask>? configure = null)
        {
            // Using a translation mask with default-on just does not seem to work properly; even if we subsequently set
            // every single field inside the mask to false, there are still records that refuse to compare equal.
            // So we have to specify every individual field we DO want to include, and default to none.
            // In addition, most fields that are lists (form lists, subrecord lists, etc.) have problems. In some cases
            // it's enough to just compare the items in the list, especially with form links, but some, like VMAD,
            // require even deeper inspection to get right.
            var mask = new Npc.TranslationMask(false, false)
            {
                // AIData is another semi-broken field in Mutagen where it ignores the mask as part of the NPC mask, but
                // compares equal when we directly compare the AIData from both NPCs. We have to ignore it in the
                // primary comparison and check it individually afterward.
                AIData = false,
                AttackRace = true,
                Class = true,
                CombatOverridePackageList = true,
                CombatStyle = true,
                Configuration = true,
                CrimeFaction = true,
                DeathItem = true,
                DefaultOutfit = true,
                DefaultPackageList = true,
                Destructible = true,
                EditorID = true,
                FaceMorph = true,
                FaceParts = true,
                FarAwayModel = true,
                FormVersion = true,
                GiftFilter = true,
                GuardWarnOverridePackageList = true,
                HairColor = true,
                HeadParts = true,
                HeadTexture = true,
                Height = true,
                MajorRecordFlagsRaw = true,
                NAM5 = true,
                Name = true,
                ObjectBounds = true,
                ObserveDeadBodyOverridePackageList = true,
                Race = true,
                ShortName = true,
                SleepingOutfit = true,
                Sound = true,
                SoundLevel = true,
                SpectatorOverridePackageList = true,
                Template = true,
                TextureLighting = true,
                TintLayers = true,
                // Version fields are tricky. Even though they should denote a change, for our purposes, they can't
                // really be treated as independent changes, because they don't do anything by themselves. If other
                // parts of the record have been modified then we'll detect those individually.
                Voice = true,
                Weight = true,
                WornArmor = true,
            };
            configure?.Invoke(mask);
            var workingEquals = x.Equals(y, mask);
            return workingEquals &&
                x.ActorEffect.SetEqualsSafe(y.ActorEffect, e => e.FormKey) &&
                x.AIData.Equals(y.AIData, new AIData.TranslationMask(true, true) { Unused = false }) &&
                x.Attacks.SetEqualsSafe(y.Attacks, a => a.AttackData?.AttackType.FormKey ?? FormKey.Null) &&
                x.Factions.SetEqualsSafe(y.Factions, f => f.Faction.FormKey) &&
                x.Items.SetEqualsSafe(y.Items, i => i.Item.Item.FormKey) &&
                x.Keywords.SetEqualsSafe(y.Keywords, k => k.FormKey) &&
                x.Packages.SetEqualsSafe(y.Packages, p => p.FormKey) &&
                x.Perks.SetEqualsSafe(y.Perks, p => p.Perk.FormKey) &&
                PlayerSkillsSame(x.PlayerSkills, y.PlayerSkills) &&
                VmadsSame(x.VirtualMachineAdapter, y.VirtualMachineAdapter);
        }

        private static bool PlayerSkillsSame(IPlayerSkillsGetter? x, IPlayerSkillsGetter? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return
                x.Equals(y, new PlayerSkills.TranslationMask(true)
                {
                    SkillOffsets = false,
                    SkillValues = false,
                    Unused = false,
                    Unused2 = false,
                }) &&
                x.SkillOffsets.SequenceEqualSafe(y.SkillOffsets) &&
                x.SkillValues.SequenceEqualSafe(y.SkillValues);
        }

        private static NpcFaceData<FormKey> ReadFaceData(INpcGetter npc)
        {
            return new NpcFaceData<FormKey>
            {
                FaceMorphs = ReadFaceMorphs(npc),
                FaceParts = ReadFaceParts(npc),
                FaceTextureSetId = npc.HeadTexture.FormKeyNullable,
                FaceTints = ReadFaceTints(npc),
                HairColorId = npc.HairColor.FormKeyNullable,
                HeadPartIds = npc.HeadParts.Select(x => x.FormKey).ToArray(),
                SkinTone = ReadSkinTone(npc),
            };
        }

        private static NpcFaceMorphs? ReadFaceMorphs(INpcGetter npc)
        {
            if (npc.FaceMorph is null)
                return null;
            return new NpcFaceMorphs
            {
                BrowsForwardBack = npc.FaceMorph.BrowsForwardVsBack,
                BrowsInOut = npc.FaceMorph.BrowsInVsOut,
                BrowsUpDown = npc.FaceMorph.BrowsUpVsDown,
                CheeksForwardBack = npc.FaceMorph.CheeksForwardVsBack,
                CheeksUpDown = npc.FaceMorph.CheeksUpVsDown,
                ChinThinWide = npc.FaceMorph.ChinNarrowVsWide,
                ChinUnderbiteOverbite = npc.FaceMorph.ChinUnderbiteVsOverbite,
                ChinUpDown = npc.FaceMorph.ChinUpVsDown,
                EyesForwardBack = npc.FaceMorph.EyesForwardVsBack,
                EyesInOut = npc.FaceMorph.EyesInVsOut,
                EyesUpDown = npc.FaceMorph.EyesUpVsDown,
                JawForwardBack = npc.FaceMorph.JawForwardVsBack,
                JawNarrowWide = npc.FaceMorph.JawNarrowVsWide,
                JawUpDown = npc.FaceMorph.JawUpVsDown,
                LipsInOut = npc.FaceMorph.LipsInVsOut,
                LipsUpDown = npc.FaceMorph.LipsUpVsDown,
                NoseLongShort = npc.FaceMorph.NoseLongVsShort,
                NoseUpDown = npc.FaceMorph.NoseUpVsDown,
            };
        }

        private static NpcFaceParts? ReadFaceParts(INpcGetter npc)
        {
            return npc.FaceParts is not null ?
                new NpcFaceParts(npc.FaceParts.Nose, npc.FaceParts.Eyes, npc.FaceParts.Mouth) : null;
        }

        private static NpcFaceTint[] ReadFaceTints(INpcGetter npc)
        {
            return npc.TintLayers
                .Where(x => x.Index.HasValue && x.Color.HasValue)
                .Select(x => new NpcFaceTint(
                    x.Index!.Value,
                    new NpcFaceTintColor(x.Color!.Value.R, x.Color.Value.G, x.Color.Value.B, x.Color.Value.A),
                    x.InterpolationValue ?? 0))
                .ToArray();
        }

        private static NpcSkinTone? ReadSkinTone(INpcGetter npc)
        {
            return npc.TextureLighting is Color c ? new NpcSkinTone(c.R, c.G, c.B) : null;
        }

        private static bool ScriptEntriesSame(IScriptEntryGetter x, IScriptEntryGetter y)
        {
            return
                x.Name == y.Name &&
                x.Flags == y.Flags &&
                x.Properties.SetEqualsSafeBy(y.Properties, p => p.Name);
        }

        private bool TryGetMasterNames(string pluginFileName, out IEnumerable<string> masterNames)
        {
            var path = Path.Combine(DataDirectory, pluginFileName);
            try
            {
                using var mod = SkyrimMod.CreateFromBinaryOverlay(ModPath.FromPath(path), skyrimRelease);
                masterNames = mod.ModHeader.MasterReferences.Select(x => x.Master.FileName.String);
                return true;
            }
            catch (Exception ex)
            {
                log.Warning(ex, "Plugin {pluginName} appears to be corrupt and cannot be loaded.", pluginFileName);
                masterNames = Enumerable.Empty<string>();
                return false;
            }
        }

        private static bool VmadsSame(IVirtualMachineAdapterGetter? x, IVirtualMachineAdapterGetter? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return
                x.ObjectFormat == y.ObjectFormat &&
                x.Version == y.Version &&
                x.Scripts.Count == y.Scripts.Count &&
                x.Scripts.OrderBy(s => s.Name)
                    .Zip(y.Scripts.OrderBy(s => s.Name))
                    .All(x => ScriptEntriesSame(x.First, x.Second));
        }
    }
}

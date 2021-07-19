using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using Focus.Apps.EasyNpc.Main;
using Focus.ModManagers;
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
        public string GameDataFolder { get; private set; }
        // Mutagen doesn't have an internal log, like XEdit Lib. (Because it doesn't need to, as it's a .NET library and
        // works with ordinary exception handling)
        public IExternalLog Log { get; init; } = new NullExternalLog();
        public IMergedPluginBuilder<FormKey> MergedPluginBuilder { get; private set; }
        public IEnumerable<ISkyrimModGetter> Mods => Environment.LoadOrder.Select(x => x.Value.Mod);
        public IModPluginMapFactory ModPluginMapFactory { get; private set; }

        private readonly ILogger log;
        private readonly IModResolver modResolver;

        public MutagenAdapter(IModResolver modResolver, ILogger log)
        {
            if (!GameLocations.TryGetDataFolder(GameRelease.SkyrimSE, out var dataFolder))
                throw new Exception("Couldn't find SkyrimSE game data folder");
            GameDataFolder = dataFolder;
            this.log = log;
            this.modResolver = modResolver;
        }

        public IEnumerable<PluginInfo> GetAvailablePlugins()
        {
            return LoadOrder.GetListings(GameRelease.SkyrimSE, GameDataFolder, true)
                .Select((x, i) => new PluginInfo(
                    x.ModKey.FileName.String, GetMasterNames(x.ModKey.FileName), x.Enabled));
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
            return listing != null && listing.Mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Master);
        }

        public Task Load(IEnumerable<string> pluginNames)
        {
            return Task.Run(() =>
            {
                var loadOrderKeys = pluginNames.Select(pluginName => ModKey.FromNameAndExtension(pluginName));
                var loadOrder = LoadOrder.Import<ISkyrimModGetter>(GameDataFolder, loadOrderKeys, GameRelease.SkyrimSE);
                var linkCache = loadOrder.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
                // If we actually managed to get here, then earlier code already managed to find the listings file.
                var listingsFile = PluginListings.GetListingsFile(GameRelease.SkyrimSE);
                var creationClubFile = CreationClubListings.GetListingsPath(GameRelease.SkyrimSE.ToCategory(), GameDataFolder);
                Environment = new GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>(
                    GameDataFolder, listingsFile, creationClubFile, loadOrder, linkCache, true);
                Environment.LinkCache.Warmup<Npc>();
                ArchiveProvider = new MutagenArchiveProvider(Environment);
                MergedPluginBuilder = new MutagenMergedPluginBuilder(Environment, log);
                ModPluginMapFactory = new MutagenModPluginMapFactory(Environment, modResolver);
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
                });
        }

        public void ReadNpcRecords(string pluginName, IDictionary<FormKey, IMutableNpc<FormKey>> cache)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var mod = GetMod(modKey);
            if (mod == null)
                return;

            var masters = mod.ModHeader.MasterReferences.Select(x => x.Master).ToHashSet();
            var npcContexts = mod.EnumerateMajorRecordContexts<INpc, INpcGetter>(Environment.LinkCache);
            foreach (var npcContext in npcContexts)
            {
                var formKey = npcContext.Record.FormKey;
                if (formKey.ModKey != modKey)
                {
                    if (!cache.TryGetValue(formKey, out var npc))
                    {
                        var declaringMod = GetMod(formKey.ModKey);
                        if (declaringMod == null)
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
                    var faceData = GetFaceOverrides(npcContext.Record, comparison, out bool affectsFaceGen);
                    var npcOverride = new NpcOverride<FormKey>(modKey.FileName)
                    {
                        FaceData = faceData,
                        FaceOverridesAffectFaceGen = affectsFaceGen,
                        ItpoPluginName = itpoPluginName,
                        ModifiesBehavior = ModifiesBehavior(npcContext, masters),
                        ModifiesBody = ModifiesBody(npcContext.Record, comparison),
                        ModifiesOutfits = ModifiesOutfits(npcContext.Record, comparison),
                        Wig = GetWigInfo(npcContext.Record),
                    };
                    npc.AddOverride(npcOverride);
                }
                else
                {
                    cache.Add(formKey, new NpcInfo<FormKey>
                    {
                        BasePluginName = modKey.FileName,
                        EditorId = npcContext.Record.EditorID,
                        IsFemale = npcContext.Record.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female),
                        Key = formKey,
                        LocalFormIdHex = formKey.ID.ToString("X6"),
                        Name = npcContext.Record.Name?.ToString(),
                    });
                }
            }
        }

        private INpcGetter GetComparisonRecord(IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> npcContext,
            out string itpoPluginName)
        {
            itpoPluginName = null;
            var previousOverride = GetPreviousOverride(npcContext);
            if (previousOverride == null)   // We were already on the master
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

        private NpcFaceData<FormKey> GetFaceOverrides(INpcGetter npc, INpcGetter comparison, out bool affectsFaceGen)
        {
            affectsFaceGen = false;
            if (comparison == null)
                return null;

            var overrideFaceData = ReadFaceData(npc);
            var previousFaceData = ReadFaceData(comparison);
            var overrideRace = npc.Race.FormKeyNullable;
            var previousRace = comparison.Race.FormKeyNullable;
            affectsFaceGen = !NpcFaceData.EqualsForFaceGen(overrideFaceData, previousFaceData);
            return (!NpcFaceData.Equals(overrideFaceData, previousFaceData) || (overrideRace != previousRace)) ?
                overrideFaceData : null;
        }

        private IEnumerable<string> GetMasterNames(string pluginFileName)
        {
            var path = Path.Combine(GameDataFolder, pluginFileName);
            using var mod = SkyrimMod.CreateFromBinaryOverlay(ModPath.FromPath(path), SkyrimRelease.SkyrimSE);
            return mod.ModHeader.MasterReferences.Select(x => x.Master.FileName.String);
        }

        private ISkyrimModGetter GetMod(ModKey key)
        {
            var listing = Environment.LoadOrder.GetIfEnabled(key);
            return listing?.Mod;
        }

        private IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> GetPreviousOverride(
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
            var raceList = headPart.ValidRaces.FormKey.AsLink<IFormListGetter>().Resolve(Environment.LinkCache);
            return raceList.Items
                .Select(x => x.FormKey.AsLink<IRaceGetter>().TryResolve(Environment.LinkCache))
                .Where(x => x != null)
                .Select(x => InferRace(x.EditorID));
        }

        private NpcWigInfo<FormKey> GetWigInfo(INpcGetter npc)
        {
            if (npc.WornArmor.IsNull)
                return null;
            var isBald = npc.HeadParts
                .Select(x => x.FormKey.AsLink<IHeadPartGetter>().Resolve(Environment.LinkCache))
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .All(x => string.IsNullOrEmpty(x.Model?.File));
            var wornArmor = npc.WornArmor.Resolve(Environment.LinkCache);
            return wornArmor.Armature
                .Select(fk => fk.Resolve(Environment.LinkCache))
                .Where(x =>
                    // Search for ONLY hair, because some sadistic modders add hair flags to other parts.
                    x.BodyTemplate.FirstPersonFlags == BipedObjectFlag.Hair ||
                    x.BodyTemplate.FirstPersonFlags == BipedObjectFlag.LongHair ||
                    x.BodyTemplate.FirstPersonFlags == (BipedObjectFlag.Hair | BipedObjectFlag.LongHair))
                .Select(x => {
                    var modelFileName = x.WorldModel?.Where(x => x != null)?.FirstOrDefault()?.File;
                    var modelName = !string.IsNullOrEmpty(modelFileName) ?
                        Path.GetFileNameWithoutExtension(modelFileName) : null;
                    return new NpcWigInfo<FormKey>(x.FormKey, modelName, isBald);
                })
                .FirstOrDefault();
        }

        private static VanillaRace InferRace(string editorId)
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
            if (previous == null)
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

        private static bool ModifiesBody(INpcGetter current, INpcGetter previous)
        {
            return previous != null && current.WornArmor == previous.WornArmor;
        }

        private static bool ModifiesOutfits(INpcGetter current, INpcGetter previous)
        {
            if (previous == null)
                return false;

            // Outfits are currently a tossup in terms of whether they should be treated with the same logic as face
            // overrides (only check PO or Declaring Master), or behavior overrides (check all masters), especially
            // we don't actually handle outfit carry-over yet, only using it as a signal for other checks.
            // This may change once outfits are actually implemented.
            return current.DefaultOutfit == previous.DefaultOutfit && current.SleepingOutfit == previous.SleepingOutfit;
        }

        private static bool NpcsSame(INpcGetter x, INpcGetter y, Action<Npc.TranslationMask> configure = null)
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
                x.ActorEffect.SequenceEqualSafe(y.ActorEffect, e => e.FormKey) &&
                x.AIData.Equals(y.AIData, new AIData.TranslationMask(true, true) { Unused = false }) &&
                x.Attacks.SequenceEqualSafe(y.Attacks, a => a.AttackData.AttackType.FormKey) &&
                x.Factions.SequenceEqualSafe(y.Factions, f => f.Faction.FormKey) &&
                x.Items.SequenceEqualSafe(y.Items, i => i.Item.Item.FormKey) &&
                x.Keywords.SequenceEqualSafe(y.Keywords, k => k.FormKey) &&
                x.Packages.SequenceEqualSafe(y.Packages, p => p.FormKey) &&
                x.Perks.SequenceEqualSafe(y.Perks, p => p.Perk.FormKey) &&
                x.PlayerSkills.Equals(y.PlayerSkills, new PlayerSkills.TranslationMask(true)
                {
                    SkillOffsets = false,
                    SkillValues = false,
                    Unused = false,
                    Unused2 = false,
                }) &&
                x.PlayerSkills.SkillOffsets.SequenceEqualSafe(y.PlayerSkills.SkillOffsets) &&
                x.PlayerSkills.SkillValues.SequenceEqualSafe(y.PlayerSkills.SkillValues) &&
                VmadsSame(x.VirtualMachineAdapter, y.VirtualMachineAdapter);
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

        private static NpcFaceMorphs ReadFaceMorphs(INpcGetter npc)
        {
            if (npc.FaceMorph == null)
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

        private static NpcFaceParts ReadFaceParts(INpcGetter npc)
        {
            return npc.FaceParts != null ?
                new NpcFaceParts(npc.FaceParts.Nose, npc.FaceParts.Eyes, npc.FaceParts.Mouth) : null;
        }

        private static NpcFaceTint[] ReadFaceTints(INpcGetter npc)
        {
            return npc.TintLayers
                .Where(x => x.Index.HasValue && x.Color.HasValue)
                .Select(x => new NpcFaceTint(
                    x.Index.Value,
                    new NpcFaceTintColor(x.Color.Value.R, x.Color.Value.G, x.Color.Value.B, x.Color.Value.A),
                    x.InterpolationValue ?? 0))
                .ToArray();
        }

        private static NpcSkinTone ReadSkinTone(INpcGetter npc)
        {
            return npc.TextureLighting is Color c ? new NpcSkinTone(c.R, c.G, c.B) : null;
        }

        private static bool ScriptEntriesSame(IScriptEntryGetter x, IScriptEntryGetter y)
        {
            return
                x.Name == y.Name &&
                x.Flags == y.Flags &&
                x.Properties.SequenceEqualSafeBy(y.Properties, p => p.Name);
        }

        private static bool VmadsSame(IVirtualMachineAdapterGetter x, IVirtualMachineAdapterGetter y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null ^ y == null)
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
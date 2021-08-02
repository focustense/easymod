using Focus.Analysis;
using Focus.Analysis.Records;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class NpcAnalyzer : IRecordAnalyzer<NpcAnalysis>
    {
        public RecordType RecordType => RecordType.Npc;

        private static readonly Func<INpcFaceMorphGetter, float>[] faceMorphValueSelectors =
            new Func<INpcFaceMorphGetter, float>[]
        {
            x => x.BrowsForwardVsBack,
            x => x.BrowsInVsOut,
            x => x.BrowsUpVsDown,
            x => x.CheeksForwardVsBack,
            x => x.CheeksUpVsDown,
            x => x.ChinNarrowVsWide,
            x => x.ChinUnderbiteVsOverbite,
            x => x.ChinUpVsDown,
            x => x.EyesForwardVsBack,
            x => x.EyesInVsOut,
            x => x.EyesUpVsDown,
            x => x.JawForwardVsBack,
            x => x.JawNarrowVsWide,
            x => x.JawUpVsDown,
            x => x.LipsInVsOut,
            x => x.LipsUpVsDown,
            x => x.NoseLongVsShort,
            x => x.NoseUpVsDown,
        };

        private readonly IGroupCache groups;
        private readonly ILogger log;

        public NpcAnalyzer(IGroupCache groups, ILogger log)
        {
            this.groups = groups;
            this.log = log;
        }

        public NpcAnalysis Analyze(string pluginName, IRecordKey key)
        {
            var group = groups.Get(pluginName, x => x.Npcs);
            var npc = group?.TryGetValue(key.ToFormKey());
            if (npc == null)
                return new() { BasePluginName = key.BasePluginName, LocalFormIdHex = key.LocalFormIdHex };
            var mod = groups.GetMod(pluginName);
            if (mod == null)
                log.Warning("Unable to find {pluginName} in the load order, some checks will be disabled.");
            var isOverride = !key.PluginEquals(pluginName);
            var overrideContexts = isOverride ?
                npc.AsLink().AllFrom(groups) : Enumerable.Empty<IKeyValue<INpcGetter, string>>();
            var previous = overrideContexts
                .SkipWhile(x => !string.Equals(x.Key, pluginName, StringComparison.CurrentCultureIgnoreCase))
                .Skip(1)
                .FirstOrDefault();
            var master = overrideContexts.LastOrDefault();
            var race = npc.Race.WinnerFrom(groups);
            return new()
            {
                BasePluginName = key.BasePluginName,
                LocalFormIdHex = key.LocalFormIdHex,
                EditorId = npc.EditorID ?? string.Empty,
                Exists = true,
                IsInjectedOrInvalid = isOverride && !groups.MasterExists(key.ToFormKey(), RecordType),
                IsOverride = isOverride,
                CanUseFaceGen = race?.Flags.HasFlag(Race.Flag.FaceGenHead) ?? false,
                ComparisonToMaster = Compare(npc, master?.Value, master?.Key),
                ComparisonToPreviousOverride = Compare(npc, previous?.Value, master?.Key),
                IsChild = race?.Flags.HasFlag(Race.Flag.Child) ?? false,
                IsFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female),
                MainHeadParts = GetMainHeadParts(npc).Select(x => x.FormKey).ToRecordKeys(),
                ModifiesBehavior = mod != null && ModifiesBehavior(npc, mod),
                UsedMeshes = Empty.ReadOnlyList<string>(),
                UsedTextures = Empty.ReadOnlyList<string>(),
                WigInfo = GetWigInfo(npc),
            };
        }

        protected NpcComparison? Compare(INpcGetter current, INpcGetter? previous, string? pluginName)
        {
            if (previous == null)
                return null;
            return new NpcComparison
            {
                ModifiesBehavior = !BehaviorsEqual(current, previous),
                ModifiesBody = !BodiesEqual(current, previous),
                ModifiesFace = !FacesEqual(current, previous),
                ModifiesHair = !HairsEqual(current, previous),
                ModifiesHeadParts = !HeadPartsEqual(current, previous),
                ModifiesOutfits = !OutfitsEqual(current, previous),
                ModifiesRace = current.Race != previous.Race,
                IsIdentical = NpcsEqual(current, previous),
                PluginName = pluginName,
            };
        }

        private static bool BehaviorsEqual(INpcGetter current, INpcGetter previous)
        {
            // For the time being, we can interpret "modifying behavior" as modifying anything that is _not_ the face.
            //
            // This is somewhat of a shotgun solution, but more precise than looking for non-ITPO records that _don't_
            // modify the face, as some mostly-behavior plugins do make edits to the face - possibly accidentally.
            var sameExcludingConfigurationFlags = NpcsEqual(current, previous, mask =>
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
            return sameExcludingConfigurationFlags &&
                // We apparently have to compare the configurations separately, because Mutagen doesn't handle it right.
                // Literally, the configurations will compare equal with the mask, while the NPCs will compare unequal
                // using a default-empty (false, false) mask including the same configuration mask.
                current.Configuration.Equals(previous.Configuration, new NpcConfiguration.TranslationMask(true, true)
                {
                    Flags = false,
                }) &&
                // Overhauls might modify the Opposite Gender Animations flag. This is considered a visual change, not
                // a behavior change. If a mod ONLY modifies this flag and nothing else about an NPC, it might be
                // ignored. There were some very old mods that did this, but they're not so common anymore, and if they
                // do exist, they probably conflict with USSEP etc.
                (current.Configuration.Flags & ~NpcConfiguration.Flag.OppositeGenderAnims) ==
                    (previous.Configuration.Flags & ~NpcConfiguration.Flag.OppositeGenderAnims);
        }

        private static bool BodiesEqual(INpcGetter x, INpcGetter y)
        {
            return x.WornArmor == y.WornArmor;
        }

        private bool FacesEqual(INpcGetter x, INpcGetter y)
        {
            return x.Equals(y, new Npc.TranslationMask(false, false)
            {
                FaceParts = true,
                HairColor = true,
                HeadTexture = true,
                TextureLighting = true,
                TintLayers = true,
            }) && FaceMorphsEqual(x.FaceMorph, y.FaceMorph) && HeadPartsEqual(x, y);
        }

        private static bool FaceMorphsEqual(INpcFaceMorphGetter? a, INpcFaceMorphGetter? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return faceMorphValueSelectors.All(m => NearlyEquals(m(a), m(b)));
        }

        private IEnumerable<IHeadPartGetter> GetMainHeadParts(INpcGetter npc)
        {
            // TODO: One issue this could miss is if a plugin has actually changed the head parts of the vanilla race.
            // It can be difficult to sort out intent here; for example, if the current/LHS plugin being compared itself
            // changes the race's head parts, then that plugin can be said to be responsible for changing the effective
            // head parts for the NPC. Or, we could say that if the effective race up to the previous/RHS plugin had
            // different head parts, then it is also a change. But what if some plugin in between the previous/RHS and
            // current/LHS changed the race's head parts and didn't change the NPCs, and the current/LHS matches that
            // in-between plugin modifying the race? Do we consider that a head part change, or not?
            var race = npc.Race.WinnerFrom(groups);
            var isFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
            var defaultHeadPartRefs = isFemale ? race?.HeadData?.Female?.HeadParts : race?.HeadData?.Male?.HeadParts;
            return (defaultHeadPartRefs ?? Enumerable.Empty<IHeadPartReferenceGetter>())
                .Select(x => x.Head.WinnerFrom(groups))
                .Concat(npc.HeadParts.Select(x => x.WinnerFrom(groups)))
                .NotNull()
                // Some parts can be "misc", i.e. don't specify what type they are, but these should always be "extra
                // parts" that are deterministically related to the "main" parts, so we can ignore them for comparison.
                .Where(x => !x.Flags.HasFlag(HeadPart.Flag.IsExtraPart))
                .GroupBy(x => x.Type)
                .Select(g => g.Last());
        }

        private NpcWigInfo? GetWigInfo(INpcGetter npc)
        {
            if (npc.WornArmor.IsNull)
                return null;
            var isBald = GetMainHeadParts(npc)
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .All(x => string.IsNullOrEmpty(x.Model?.File));
            var wornArmor = npc.WornArmor.WinnerFrom(groups);
            if (wornArmor == null)
                return null;
            var isFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
            return wornArmor.Armature
                .Select(fk => fk.WinnerFrom(groups))
                .NotNull()
                .Where(x =>
                    // Search for ONLY hair, because some sadistic mod authors add hair flags to other parts.
                    x.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.Hair ||
                    x.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.LongHair ||
                    x.BodyTemplate?.FirstPersonFlags == (BipedObjectFlag.Hair | BipedObjectFlag.LongHair))
                .Select(x => {
                    var modelFileName = x.WorldModel?.PickGender(isFemale)?.File;
                    var modelName = !string.IsNullOrEmpty(modelFileName) ?
                        Path.GetFileNameWithoutExtension(modelFileName) : null;
                    return new NpcWigInfo(x.FormKey.ToRecordKey(), modelName, isBald);
                })
                .FirstOrDefault();
        }

        private bool HairsEqual(INpcGetter x, INpcGetter y)
        {
            var lhsHair = GetMainHeadParts(x).SingleOrDefault(x => x.Type == HeadPart.TypeEnum.Hair);
            var rhsHair = GetMainHeadParts(y).SingleOrDefault(x => x.Type == HeadPart.TypeEnum.Hair);
            return lhsHair?.FormKey == rhsHair?.FormKey;
        }

        private bool HeadPartsEqual(INpcGetter x, INpcGetter y)
        {
            var lhsParts = GetMainHeadParts(x).Select(x => x.FormKey.ToRecordKey());
            var rhsParts = GetMainHeadParts(y).Select(x => x.FormKey.ToRecordKey());
            return new HashSet<RecordKey>(lhsParts).SetEquals(rhsParts);
        }

        private bool ModifiesBehavior(INpcGetter npc, ISkyrimModGetter contextMod)
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
            var masterKeys = contextMod.ModHeader.MasterReferences
                .Select(x => x.Master.FileName.String)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var previousOverrides = npc.AsLink()
                .AllFrom(groups)
                .SkipWhile(x => !string.Equals(x.Key, contextMod.ModKey.FileName, StringComparison.CurrentCultureIgnoreCase))
                .Skip(1)
                .Where(x => masterKeys.Contains(x.Key));
            return previousOverrides.All(x => !BehaviorsEqual(npc, x.Value));
        }

        private static bool NearlyEquals(float x, float y)
        {
            return Math.Abs(x - y) > 3 * float.Epsilon;
        }

        private static bool NpcsEqual(INpcGetter x, INpcGetter y, Action<Npc.TranslationMask>? configure = null)
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
                x.Attacks.SequenceEqualSafe(y.Attacks, a => a.AttackData?.AttackType.FormKey) &&
                x.Factions.SequenceEqualSafe(y.Factions, f => f.Faction.FormKey) &&
                x.Items.SequenceEqualSafe(y.Items, i => i.Item.Item.FormKey) &&
                x.Keywords.SequenceEqualSafe(y.Keywords, k => k.FormKey) &&
                x.Packages.SequenceEqualSafe(y.Packages, p => p.FormKey) &&
                x.Perks.SequenceEqualSafe(y.Perks, p => p.Perk.FormKey) &&
                PlayerSkillsEqual(x.PlayerSkills, y.PlayerSkills) &&
                VmadsEqual(x.VirtualMachineAdapter, y.VirtualMachineAdapter);
        }

        private static bool OutfitsEqual(INpcGetter x, INpcGetter y)
        {
            // Outfits are currently a tossup in terms of whether they should be treated with the same logic as face
            // overrides (only check PO or Declaring Master), or behavior overrides (check all masters), especially
            // we don't actually handle outfit carry-over yet, only using it as a signal for other checks.
            // This may change once outfits are actually implemented.
            return x.DefaultOutfit == y.DefaultOutfit && x.SleepingOutfit == y.SleepingOutfit;
        }

        private static bool PlayerSkillsEqual(IPlayerSkillsGetter? x, IPlayerSkillsGetter? y)
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

        private static bool ScriptEntriesSame(IScriptEntryGetter x, IScriptEntryGetter y)
        {
            return
                x.Name == y.Name &&
                x.Flags == y.Flags &&
                x.Properties.SequenceEqualSafeBy(y.Properties, p => p.Name);
        }

        private static bool VmadsEqual(IVirtualMachineAdapterGetter? x, IVirtualMachineAdapterGetter? y)
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
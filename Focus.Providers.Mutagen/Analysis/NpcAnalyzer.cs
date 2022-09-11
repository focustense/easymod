using Focus.Analysis.Records;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class NpcAnalyzer : IRecordAnalyzer<NpcAnalysis>
    {
        public interface IArmorAddonHelper
        {
            bool IsDefaultSkin(IArmorAddonGetter addon, IFormLinkGetter<IRaceGetter> race);
            bool ReplacesHair(IArmorAddonGetter addon);
            bool SupportsRace(IArmorAddonGetter addon, IFormLinkGetter<IRaceGetter> race);
        }

        public class ArmorAddonHelper : IArmorAddonHelper
        {
            private readonly ConcurrentDictionary<FormKey, HashSet<FormKey>> defaultRaceArmorAddons = new();
            private readonly IGroupCache groups;

            public ArmorAddonHelper(IGroupCache groups)
            {
                this.groups = groups;
            }

            public bool IsDefaultSkin(IArmorAddonGetter addon, IFormLinkGetter<IRaceGetter> race)
            {
                return GetDefaultRaceArmorAddons(race).Contains(addon.FormKey);

            }

            public bool ReplacesHair(IArmorAddonGetter addon)
            {
                return
                    addon.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.Hair ||
                    addon.BodyTemplate?.FirstPersonFlags == BipedObjectFlag.LongHair ||
                    addon.BodyTemplate?.FirstPersonFlags == (BipedObjectFlag.Hair | BipedObjectFlag.LongHair);
            }

            public bool SupportsRace(IArmorAddonGetter addon, IFormLinkGetter<IRaceGetter> race)
            {
                return addon.Race.FormKey == race.FormKey || addon.AdditionalRaces.Contains(race.FormKey);
            }

            private HashSet<FormKey> GetDefaultRaceArmorAddons(IFormLinkGetter<IRaceGetter> raceLink)
            {
                return defaultRaceArmorAddons.GetOrAdd(raceLink.FormKey, _ =>
                {
                    var race = raceLink.MasterFrom(groups);
                    var skin = race?.Skin.MasterFrom(groups);
                    return skin?.Armature?.Select(x => x.FormKey)?.ToHashSet() ?? new();
                });
            }
        }

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

        private readonly IArmorAddonHelper armorAddonHelper;
        private readonly IAssetPathExtractor<INpcGetter>? assetPathExtractor;
        private readonly IGroupCache groups;
        private readonly IReferenceChecker<INpcGetter>? referenceChecker;

        public NpcAnalyzer(
            IGroupCache groups, IReferenceChecker<INpcGetter>? referenceChecker = null,
            IAssetPathExtractor<INpcGetter>? assetPathExtractor = null)
            : this(groups, new ArmorAddonHelper(groups), referenceChecker, assetPathExtractor) { }

        public NpcAnalyzer(
            IGroupCache groups, IArmorAddonHelper armorAddonHelper,
            IReferenceChecker<INpcGetter>? referenceChecker = null,
            IAssetPathExtractor<INpcGetter>? assetPathExtractor = null)
        {
            this.armorAddonHelper = armorAddonHelper;
            this.assetPathExtractor = assetPathExtractor;
            this.groups = groups;
            this.referenceChecker = referenceChecker;
        }

        public NpcAnalysis Analyze(string pluginName, IRecordKey key)
        {
            var group = groups.Get(pluginName, x => x.Npcs);
            var npc = group?.TryGetValue(key.ToFormKey());
            if (npc is null)
                return new() { BasePluginName = key.BasePluginName, LocalFormIdHex = key.LocalFormIdHex };

            var isOverride = !key.PluginEquals(pluginName);
            var relations = isOverride ? groups.GetRelations(npc.ToLink(), pluginName) : new();
            var comparisonToMasters = relations.Masters.Select(x => Compare(npc, x.Value, x.Key)).ToList().AsReadOnly();
            var comparisonToBase = FindComparison(comparisonToMasters, relations.Base?.Key) ??
                Compare(npc, relations.Base?.Value, relations.Base?.Key);
            var comparisonToPrevious = FindComparison(comparisonToMasters, relations.Previous?.Key) ??
                Compare(npc, relations.Previous?.Value, relations.Previous?.Key);

            var race = npc.Race.WinnerFrom(groups);
            return new()
            {
                BasePluginName = key.BasePluginName,
                LocalFormIdHex = key.LocalFormIdHex,
                EditorId = npc.EditorID ?? string.Empty,
                Exists = true,
                InvalidPaths = referenceChecker.SafeCheck(npc),
                IsInjectedOrInvalid = isOverride && !groups.MasterExists(key.ToFormKey(), RecordType),
                IsOverride = isOverride,
                CanUseFaceGen = race?.Flags.HasFlag(Race.Flag.FaceGenHead) ?? false,
                ComparisonToBase = comparisonToBase,
                ComparisonToMasters = comparisonToMasters,
                ComparisonToPreviousOverride = comparisonToPrevious,
                IsAudioTemplate = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.UseTemplate),
                IsChild = race?.Flags.HasFlag(Race.Flag.Child) ?? false,
                IsFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female),
                MainHeadParts = GetMainHeadParts(npc).Select(x => x.FormKey).ToRecordKeys(),
                Name = npc.Name?.String ?? string.Empty,
                ReferencedAssets = (assetPathExtractor?.GetReferencedAssets(npc) ?? Enumerable.Empty<AssetReference>())
                    .ToList().AsReadOnly(),
                SkinKey = GetSkin(npc).FormKeyNullable?.ToSystemNullable()?.ToRecordKey(),
                TemplateInfo = GetTemplateInfo(npc),
                WigInfo = GetWigInfo(npc),
            };
        }

        [return: NotNullIfNotNull("previous")]
        protected NpcComparison? Compare(INpcGetter current, INpcGetter? previous, string? pluginName)
        {
            if (previous is null || ReferenceEquals(previous, current))
                return null;
            return new NpcComparison
            {
                ModifiesBehavior = !BehaviorsEqual(current, previous),
                ModifiesSkin = !SkinsEqual(current, previous),
                ModifiesFace = !FacesEqual(current, previous),
                ModifiesHair = !HairsEqual(current, previous),
                ModifiesHeadParts = !HeadPartsEqual(current, previous),
                ModifiesOutfits = !OutfitsEqual(current, previous),
                ModifiesRace = current.Race.FormKey != previous.Race.FormKey,
                ModifiesScales = !ScalesEqual(current, previous),
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

        private bool FacesEqual(INpcGetter x, INpcGetter y)
        {
            return
                x.Equals(y, new Npc.TranslationMask(false, false)
                {
                    FaceParts = true,
                    HairColor = true,
                    HeadTexture = true,
                    TextureLighting = true,
                }) &&
                FaceMorphsEqual(x.FaceMorph, y.FaceMorph) &&
                // Mutagen bug: does not honor the Tint Layers mask if it specified to ignore the Preset.
                TintLayersEqual(x.TintLayers, y.TintLayers) &&
                HeadPartsEqual(x, y);
        }

        private static bool FaceMorphsEqual(INpcFaceMorphGetter? a, INpcFaceMorphGetter? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return faceMorphValueSelectors.All(m => NearlyEquals(m(a), m(b)));
        }

        private static NpcComparison? FindComparison(IEnumerable<NpcComparison> comparisons, string? pluginName)
        {
            return !string.IsNullOrEmpty(pluginName) ?
                comparisons.FirstOrDefault(x =>
                    string.Equals(x.PluginName, pluginName, StringComparison.CurrentCultureIgnoreCase)) :
                null;
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
                .GroupBy(x => x.Type)
                .SelectMany(g => IsSecondaryPart(g.Key) ? g.Cast<IHeadPartGetter>() : new[] { g.Last() });
        }

        private IFormLinkGetter<IArmorGetter> GetSkin(INpcGetter npc)
        {
            if (!npc.WornArmor.IsNull)
                return npc.WornArmor;
            // We could use WinnerFrom instead of MasterFrom here, but the latter gives us a better picture of
            // "originally intended skin" for use in comparisons. Skins are not like head parts, it's not big news if a
            // mod changes the default skin of a race, because the new skin should be broadly compatible with the old
            // skin if the mod author knew what he was doing. This is more useful for telling us, for example, that an
            // "ElderRace" NPC without a custom skin should normally end up with the same skin as a "Nord" NPC without
            // a custom skin, so if a mod changes the race from Elder to Nord, the body textures should be compatible,
            // even if some other mod decided to tweak one or both of them.
            var race = !npc.Race.IsNull ? npc.Race.MasterFrom(groups) : null;
            return race?.Skin ?? FormLinkGetter<IArmorGetter>.Null;
        }

        private NpcTemplateInfo? GetTemplateInfo(INpcGetter npc)
        {
            if (npc.Template.IsNull)
                return null;
            var targetType = NpcTemplateTargetType.Invalid;
            if (npc.Template.FormKey.ToLink<INpcGetter>().WinnerFrom(groups) is not null)
                targetType = NpcTemplateTargetType.Npc;
            else if (npc.Template.FormKey.ToLink<ILeveledNpcGetter>().WinnerFrom(groups) is not null)
                targetType = NpcTemplateTargetType.LeveledNpc;
            var inheritsTraits = npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits);
            return new NpcTemplateInfo(npc.Template.FormKey.ToRecordKey(), targetType, inheritsTraits);
        }

        private NpcWigInfo? GetWigInfo(INpcGetter npc)
        {
            if (npc.WornArmor.IsNull)
                return null;
            var isBald = GetMainHeadParts(npc)
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .All(x => string.IsNullOrEmpty(x.Model?.File));
            var wornArmor = npc.WornArmor.WinnerFrom(groups);
            if (wornArmor is null)
                return null;
            // Some mods make a real mess of this. We need to know if a Worn Armor is ONLY intended to modify the hair.
            // However, certain authors have chosen to jam in all sorts of unnecessary parts for other races, "just in
            // case", usually referencing vanilla addons. Our goal is to gracefully ignore those without missing too
            // many legitimate wigs.
            var allAddons = wornArmor.Armature
                .Select(fk => fk.WinnerFrom(groups))
                .NotNull()
                .Where(x => armorAddonHelper.SupportsRace(x, npc.Race) && !armorAddonHelper.IsDefaultSkin(x, npc.Race))
                .ToList();
            if (allAddons.Any(x => !armorAddonHelper.ReplacesHair(x)))
                return null;
            var wigAddons = allAddons.Where(x => armorAddonHelper.ReplacesHair(x)).ToList();
            if (wigAddons.Count != 1)
                return null;
            var isFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
            return wigAddons
                .Select(x => {
                    var modelFileName = x.WorldModel?.PickGender(isFemale)?.File;
                    var modelName = !string.IsNullOrEmpty(modelFileName) ?
                        Path.GetFileNameWithoutExtension(modelFileName) : null;
                    return new NpcWigInfo(x.FormKey.ToRecordKey(), x.EditorID, modelName, isBald);
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

        private static bool IsSecondaryPart(HeadPart.TypeEnum? headPartType)
        {
            // Marks (scars) are confirmed to be a "secondary" or "additional" head part, meaning multiple can be added.
            // Unsure if any other types are in the same category.
            return headPartType == HeadPart.TypeEnum.Scars;
        }

        private static bool NearlyEquals(float x, float y)
        {
            return Math.Abs(x - y) < 3 * float.Epsilon;
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
                x.ActorEffect.SetEqualsSafe(y.ActorEffect, e => e.FormKey) &&
                x.AIData.Equals(y.AIData, new AIData.TranslationMask(true, true) { Unused = false }) &&
                x.Attacks.SetEqualsSafe(y.Attacks, a => a.AttackData?.AttackType.FormKey) &&
                x.Factions.SetEqualsSafe(y.Factions, f => f.Faction.FormKey) &&
                x.Items.SetEqualsSafe(y.Items, i => i.Item.Item.FormKey) &&
                x.Keywords.SetEqualsSafe(y.Keywords, k => k.FormKey) &&
                x.Packages.SequenceEqualSafe(y.Packages, p => p.FormKey) &&
                x.Perks.SetEqualsSafe(y.Perks, p => p.Perk.FormKey) &&
                PlayerSkillsEqual(x.PlayerSkills, y.PlayerSkills) &&
                VmadsEqual(x.VirtualMachineAdapter, y.VirtualMachineAdapter);
        }

        private static bool OutfitsEqual(INpcGetter x, INpcGetter y)
        {
            // Outfits are currently a tossup in terms of whether they should be treated with the same logic as face
            // overrides (only check PO or Declaring Master), or behavior overrides (check all masters), especially
            // we don't actually handle outfit carry-over yet, only using it as a signal for other checks.
            // This may change once outfits are actually implemented.
            return
                x.DefaultOutfit.FormKey == y.DefaultOutfit.FormKey &&
                x.SleepingOutfit.FormKey == y.SleepingOutfit.FormKey;
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

        private static bool ScalesEqual(INpcGetter current, INpcGetter previous)
        {
            return NearlyEquals(current.Height, previous.Height) && NearlyEquals(current.Weight, previous.Weight);
        }

        private static bool ScriptEntriesSame(IScriptEntryGetter x, IScriptEntryGetter y)
        {
            return
                x.Name == y.Name &&
                x.Flags == y.Flags &&
                x.Properties.SetEqualsSafeBy(y.Properties, p => p.Name);
        }

        private bool SkinsEqual(INpcGetter x, INpcGetter y)
        {
            return GetSkin(x).FormKey == GetSkin(y).FormKey;
        }

        private static bool TintLayersEqual(IReadOnlyList<ITintLayerGetter> a, IReadOnlyList<ITintLayerGetter> b)
        {
            if (a.Count != b.Count)
                return false;
            return a.OrderBy(x => x.Index)
                .Zip(b.OrderBy(x => x.Index))
                .All(x => TintLayersEqual(x.First, x.Second));
        }

        private static bool TintLayersEqual(ITintLayerGetter x, ITintLayerGetter y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return x.Equals(y, new TintLayer.TranslationMask(false, false)
            {
                Color = true,
                Index = true,
                InterpolationValue = true,
            });
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
using Focus.Apps.EasyNpc.Configuration;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class RecordImporter
    {
        public delegate RecordImporter Factory(ISkyrimMod mergedMod);

        private readonly Dictionary<FormKey, IMajorRecord> duplicatedRecords = new();
        private readonly IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment;
        private readonly ILogger log;
        private readonly HashSet<ModKey> masters = new();
        private readonly ISkyrimMod mergedMod;
        // We need to keep track of these ourselves, because merged records won't appear in the original LinkCache.
        private readonly Dictionary<FormKey, IMajorRecord> mergedRecords = new();
        private readonly Lazy<IReadOnlyList<IFormLinkGetter<IArmorAddonGetter>>> raceTransformationAddons;

        public RecordImporter(
            IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment, IAppSettings appSettings,
            ISkyrimMod mergedMod, ILogger log)
        {
            this.environment = environment;
            this.log = log;
            this.mergedMod = mergedMod;

            var raceTransformationKeys = appSettings.RaceTransformationKeys.Select(key => key.ToFormKey()).ToList();
            raceTransformationAddons = new(() => GetDefaultRaceArmorAddons(raceTransformationKeys).ToList(), true);
        }

        public void AddArmorRace(
            IFormLinkGetter<IArmorGetter> armorLink, IFormLinkGetter<IRaceGetter> originalRace,
            IFormLinkGetter<IRaceGetter> actualRace)
        {
            var armorKey = armorLink.FormKey;
            if (armorKey.IsNull || armorKey.ModKey != mergedMod.ModKey || originalRace.FormKey == actualRace.FormKey)
                return;
            var armor = GetIfMerged<Armor>(armorKey);
            if (armor is null)
                return;
            var affectedAddons = armor.Armature
                .Select(x => GetIfMerged<ArmorAddon>(x.FormKey))
                .NotNull()
                .Where(x => x.Race.FormKey == originalRace.FormKey || x.AdditionalRaces.Contains(originalRace.FormKey));
            foreach (var addon in affectedAddons)
                if (addon.Race.FormKey != actualRace.FormKey && !addon.AdditionalRaces.Contains(actualRace.FormKey))
                    addon.AdditionalRaces.Add(actualRace.FormKey);
        }

        public void AddFormListKey(IFormLinkGetter<IFormListGetter> formListLink, FormKey itemKey)
        {
            var formListKey = formListLink.FormKey;
            if (formListKey.IsNull || formListKey.ModKey != mergedMod.ModKey)
                return;
            var formList = GetIfMerged<FormList>(formListKey);
            if (formList is not null && !formList.Items.Contains(itemKey))
                formList.Items.Add(itemKey);
        }

        public void AddHeadPartRace(IFormLinkGetter<IHeadPartGetter> headPartLink, IFormLinkGetter<IRaceGetter> raceLink)
        {
            if (raceLink.IsNull)
                return;
            var headPartKey = headPartLink.FormKey;
            if (headPartKey.IsNull || headPartKey.ModKey != mergedMod.ModKey)
                return;
            var headPart = GetIfMerged<HeadPart>(headPartKey);
            if (headPart is not null)
                AddFormListKey(headPart.ValidRaces, raceLink.FormKey);
        }

        public void AddMaster(string pluginName)
        {
            masters.Add(ModKey.FromNameAndExtension(pluginName));
        }

        public FormKey? Import<T, TGetter>(IFormLinkGetter<TGetter> link, Func<ISkyrimMod, IGroup<T>> groupSelector)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            Action<T>? defaultSetup = null;
            Action<T>? masterSetup = null;
            // There's probably a switch expression that's better for this, but Mutagen doesn't make it obvious.
            if (typeof(IHeadPartGetter).IsAssignableFrom(link.Type))
                defaultSetup = x => ImportHeadPartDependencies((HeadPart)(SkyrimMajorRecord)x);
            else if (typeof(IArmorGetter).IsAssignableFrom(link.Type))
            {
                defaultSetup = x => ImportWornArmorDependencies((Armor)(SkyrimMajorRecord)x);
                masterSetup = x => AddRaceTransformationAddons((Armor)(SkyrimMajorRecord)x);
            }
            else if (typeof(IArmorAddonGetter).IsAssignableFrom(link.Type))
                defaultSetup = x => ImportWornArmorAddonDependencies((ArmorAddon)(SkyrimMajorRecord)x);
            else if (typeof(IArtObjectGetter).IsAssignableFrom(link.Type))
                defaultSetup = x => ImportArtObjectDependencies((ArtObject)(SkyrimMajorRecord)x);
            return Import(link, groupSelector(mergedMod), defaultSetup, masterSetup);
        }

        public T? TryResolve<T>(FormKey key) where T : class, IMajorRecordGetter
        {
            var record = GetIfMerged<T>(key) ?? key.AsLink<T>().TryResolve(environment.LinkCache);
            if (record == null)
            {
                var recordTypeName = MutagenExtensions.GetRecordTypeName<T>();
                log.Warning(
                    "{linkType:l} {referencedFormKey} is not in the merge mod and could not be resolved anywhere " +
                    "else in the load order. The current NPC will not convert correctly.",
                    recordTypeName, key);
            }
            return record;
        }

        private void AddRaceTransformationAddons(Armor armor)
        {
            armor.EditorID += "Patched";
            foreach (var addon in raceTransformationAddons.Value)
                if (!armor.Armature.Contains(addon.FormKey))
                    armor.Armature.Add(addon);
        }

        private IEnumerable<IFormLinkGetter<IArmorAddonGetter>> GetDefaultRaceArmorAddons(
            IEnumerable<FormKey> raceKeys)
        {
            foreach (var raceKey in raceKeys)
            {
                var race = raceKey.AsLinkGetter<IRaceGetter>().TryResolve(environment.LinkCache);
                if (race is null)
                    continue;
                var skin = race.Skin.TryResolve(environment.LinkCache);
                if (skin is null)
                    continue;
                foreach (var addon in skin.Armature)
                    yield return addon;
            }
        }

        private T? GetIfMerged<T>(FormKey key) where T : IMajorRecordGetter
        {
            return mergedRecords.TryGetValue(key, out var merged) ? (T)merged : default;
        }

        private FormKey? Import<T, TGetter>(
            IFormLinkGetter<TGetter>? link, IGroup<T> group, Action<T>? defaultSetup = null,
            Action<T>? masterSetup = null)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            if (link is null || link.IsNull)
                return null;

            log.Debug("Clone requested for {Type} ({FormKey})", link.Type.Name, link.FormKey);
            var isFromMaster = masters.Contains(link.FormKey.ModKey) && !IsInjected<T, TGetter>(link);
            if (isFromMaster && masterSetup is null)
            {
                log.Debug("Record {FormKey} is already provided by the merged plugin's masters.", link.FormKey);
                return link.FormKey;
            }
            else if (duplicatedRecords.TryGetValue(link.FormKey, out var cachedCopy))
            {
                log.Debug(
                    "Record {FormKey} has already been cloned as {ClonedFormKey}.",
                    link.FormKey, cachedCopy.FormKey);
                return cachedCopy.FormKey;
            }
            else
            {
                log.Debug("Beginning clone for record {FormKey}", link.FormKey);
                var sourceRecord = link.Resolve(environment.LinkCache);
                var duplicateRecord = group.AddNew();
                duplicateRecord.DeepCopyIn(sourceRecord);
                duplicatedRecords.Add(link.FormKey, duplicateRecord);
                mergedRecords.Add(duplicateRecord.FormKey, duplicateRecord);
                log.Debug(
                    "Clone completed for {FormKey} -> {ClonedFormKey}; performing additional setup",
                    link.FormKey, duplicateRecord.FormKey);
                if (isFromMaster)
                    masterSetup?.Invoke(duplicateRecord);
                else
                    defaultSetup?.Invoke(duplicateRecord);
                log.Information(
                    "Cloned {Type} '{EditorId}' ({FormKey}) as {ClonedFormKey}",
                    link.Type.Name, sourceRecord.EditorID, link.FormKey, duplicateRecord.FormKey);
                return duplicateRecord.FormKey;
            }
        }

        private void ImportArtObjectDependencies(ArtObject art)
        {
            log.Debug("Processing art object {FormKey} '{EditorId}' for cloning", art.FormKey, art.EditorID);

            ReplaceAlternateTextures(art.Model?.AlternateTextures);

            log.Information("Finished processing cloned art object {FormKey} '{EditorId}'", art.EditorID, art.FormKey);
        }

        private void ImportEmptyFormList(FormList formList)
        {
            formList.Items.Clear();
        }

        private void ImportHeadPartDependencies(HeadPart headPart)
        {
            log.Debug(
                "Processing head part {FormKey} '{EditorId}' for cloning",
                headPart.FormKey, headPart.EditorID);
            headPart.Flags &= ~HeadPart.Flag.Playable;
            headPart.Color.SetTo(Import(headPart.Color, mergedMod.Colors));
            headPart.TextureSet.SetTo(Import(headPart.TextureSet, mergedMod.TextureSets));
            headPart.ValidRaces.SetTo(Import(headPart.ValidRaces, mergedMod.FormLists, ImportEmptyFormList));
            ReplaceAlternateTextures(headPart.Model?.AlternateTextures);
            ReplaceList(headPart.ExtraParts, mergedMod.HeadParts, ImportHeadPartDependencies);
            log.Information(
                "Finished processing cloned head part {FormKey} '{EditorId}'",
                headPart.EditorID, headPart.FormKey);
        }

        private void ImportTextureSetList(FormList list)
        {
            var textureSetLinks = list.Items.Where(x => x.Type == typeof(ITextureSetGetter)).ToList();
            list.Items.Clear();
            foreach (var textureSetLink in textureSetLinks)
            {
                var mergedTextureSetKey = Import(textureSetLink, mergedMod.TextureSets);
                if (mergedTextureSetKey != null)
                    list.Items.Add(mergedTextureSetKey.Value.AsLinkGetter<ITextureSetGetter>());
            }
        }

        private void ImportWornArmorAddonDependencies(ArmorAddon addon)
        {
            log.Debug("Processing armor addon {FormKey} '{EditorId}' for cloning", addon.FormKey, addon.EditorID);

            // NPCs aren't going to inherit any custom races unless they're in the masters already, so it should be safe
            // to remove those from addons. Other races can stay.
            addon.AdditionalRaces.RemoveAll(x => x.IsNull || !masters.Contains(x.FormKey.ModKey));

            // Footstep sets have a not-very-shallow tree to import. Use a similar strategy as the race: if not included
            // in the existing masters, remove it (i.e. replace with the default).
            if (!addon.FootstepSound.IsNull && !masters.Contains(addon.FootstepSound.FormKey.ModKey))
            {
                if (addon.BodyTemplate?.FirstPersonFlags.HasFlag(BipedObjectFlag.Feet) == true)
                    addon.FootstepSound.SetTo(
                        environment.LinkCache.Resolve<IFootstepSetGetter>("FSTBarefootFootstepSet"));
                else
                    // If it's not even a feet addon, it shouldn't have footsteps.
                    addon.FootstepSound.Clear();
            }

            // Allow any playable race to use the addon. We don't know which NPCs will try.
            var defaultRace = environment.LinkCache.Resolve<IRaceGetter>("DefaultRace");
            var racesToKeep = addon.AdditionalRaces.Prepend(addon.Race)
                .NotNull()
                .Select(x => x.FormKey)
                .Where(x => !x.IsNull && x != defaultRace.FormKey && masters.Contains(x.ModKey))
                .Distinct()
                .ToList();
            addon.Race.SetTo(defaultRace);
            addon.AdditionalRaces.Clear();
            foreach (var race in racesToKeep)
                addon.AdditionalRaces.Add(race);

            addon.ArtObject.SetTo(Import(addon.ArtObject, mergedMod.ArtObjects, ImportArtObjectDependencies));
            ReplaceAlternateTextures(addon.FirstPersonModel?.Female?.AlternateTextures);
            ReplaceAlternateTextures(addon.FirstPersonModel?.Male?.AlternateTextures);
            if (addon.SkinTexture is not null)
            {
                var maleSkinTexture = Import(addon.SkinTexture?.Male, mergedMod.TextureSets) ?? FormKey.Null;
                var femaleSkinTexture = Import(addon.SkinTexture?.Female, mergedMod.TextureSets) ?? FormKey.Null;
                addon.SkinTexture = new GenderedItem<IFormLinkNullableGetter<ITextureSetGetter>>(
                    maleSkinTexture.AsLinkGetter<ITextureSetGetter>().AsNullable(),
                    femaleSkinTexture.AsLinkGetter<ITextureSetGetter>().AsNullable());
            }
            // Form lists (skin texture swap list) can be pretty brutal to merge as a form list can technically contain
            // anything at all. However, we can reduce the complexity without breaking anything important (hopefully) by
            // only copying the TXST references inside - since this is supposed to be a list of textures.
            if (addon.TextureSwapList is not null)
            {
                var maleSwapList =
                    Import(addon.TextureSwapList?.Male, mergedMod.FormLists, ImportTextureSetList) ?? FormKey.Null;
                var femaleSwapList =
                    Import(addon.TextureSwapList?.Female, mergedMod.FormLists, ImportTextureSetList) ?? FormKey.Null;
                addon.TextureSwapList = new GenderedItem<IFormLinkNullableGetter<IFormListGetter>>(
                    maleSwapList.AsLinkGetter<IFormListGetter>().AsNullable(),
                    femaleSwapList.AsLinkGetter<IFormListGetter>().AsNullable());
            }
            ReplaceAlternateTextures(addon.WorldModel?.Female?.AlternateTextures);
            ReplaceAlternateTextures(addon.WorldModel?.Male?.AlternateTextures);

            log.Information(
                "Finished processing cloned armor addon {FormKey} '{EditorId}'", addon.EditorID, addon.FormKey);
        }

        private void ImportWornArmorDependencies(Armor armor)
        {
            log.Debug("Processing armor {FormKey} '{EditorId}' for cloning", armor.FormKey, armor.EditorID);

            // We only care about the visuals. "Worn Armors" shouldn't have any of these fields.
            armor.AlternateBlockMaterial.Clear();
            armor.BashImpactDataSet.Clear();
            armor.EquipmentType.Clear();
            armor.ObjectEffect.Clear();
            armor.PickUpSound.Clear();
            armor.PutDownSound.Clear();
            armor.VirtualMachineAdapter = null;

            // There's probably a mod out there somewhere that tries to attach a Destructible to a Worn Armor, but if
            // so, then it's an extreme edge case and the effects of removing it will rarely be encountered.
            armor.Destructible = null;

            // Setting to default race should allow all races to "equip" the armor, meaning it should be compatible with
            // whatever race an NPC happens to be inheriting from the default (not face) plugin.
            armor.Race.SetTo(environment.LinkCache.Resolve<IRaceGetter>("DefaultRace"));

            // Required for beast transformations to work: https://github.com/focustense/easymod/issues/133
            AddRaceTransformationAddons(armor);

            ReplaceList(armor.Armature, mergedMod.ArmorAddons, ImportWornArmorAddonDependencies);
            ReplaceList(armor.Keywords, mergedMod.Keywords);
            armor.TemplateArmor.SetTo(Import(armor.TemplateArmor, mergedMod.Armors, ImportWornArmorDependencies));
            ReplaceAlternateTextures(armor.WorldModel?.Female?.Model?.AlternateTextures);
            ReplaceAlternateTextures(armor.WorldModel?.Male?.Model?.AlternateTextures);

            log.Information("Finished processing cloned armor {FormKey} '{EditorId}'", armor.EditorID, armor.FormKey);
        }

        private bool IsInjected<T, TGetter>(IFormLinkGetter<TGetter> link)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            var baseModKey = link.FormKey.ModKey;
            // Not the most efficient way to do this check at scale, but seems "good enough" (not more than 1-2
            // seconds) at this time. Optimal time would be to cache all the relevant top level groups of all the base
            // mods, and check their contents directly.
            return link.ResolveAllContexts<ISkyrimMod, ISkyrimModGetter, T, TGetter>(environment.LinkCache)
                .All(x => x.ModKey != baseModKey);
        }

        private void ReplaceAlternateTextures(IEnumerable<AlternateTexture>? altTextures)
        {
            if (altTextures is null)
                return;
            foreach (var altTexture in altTextures)
                altTexture.NewTexture.SetTo(Import(altTexture.NewTexture, mergedMod.TextureSets));
        }

        private void ReplaceList<T, TGetter>(
            ExtendedList<IFormLinkGetter<TGetter>>? list, IGroup<T> group, Action<T>? setup = null)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            if (list is null)
                return;
            var mergedItems = list
                .Select(x => Import(x, group, setup))
                .NotNull()
                .ToList();
            list.Clear();
            list.AddRange(mergedItems);
        }
    }
}

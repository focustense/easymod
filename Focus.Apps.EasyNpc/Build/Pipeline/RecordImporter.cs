using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
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

        public RecordImporter(
            IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment, ISkyrimMod mergedMod, ILogger log)
        {
            this.environment = environment;
            this.log = log;
            this.mergedMod = mergedMod;
        }

        public void AddMaster(string pluginName)
        {
            masters.Add(ModKey.FromNameAndExtension(pluginName));
        }

        public FormKey? Import<T, TGetter>(IFormLinkGetter<TGetter> link, Func<ISkyrimMod, IGroup<T>> groupSelector)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            Action<T>? setup = null;
            // There's probably a switch expression that's better for this, but Mutagen doesn't make it obvious.
            if (typeof(IHeadPartGetter).IsAssignableFrom(link.Type))
                setup = x => ImportHeadPartDependencies((HeadPart)(SkyrimMajorRecord)x);
            return Import(link, groupSelector(mergedMod), setup);
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

        private T? GetIfMerged<T>(FormKey key) where T : IMajorRecordGetter
        {
            return mergedRecords.TryGetValue(key, out var merged) ? (T)merged : default;
        }

        private FormKey? Import<T, TGetter>(
            IFormLinkGetter<TGetter> link, IGroup<T> group, Action<T>? setup = null)
            where T : SkyrimMajorRecord, TGetter
            where TGetter : class, ISkyrimMajorRecordGetter
        {
            if (link.FormKeyNullable == null)
                return null;

            log.Debug("Clone requested for {Type} ({FormKey})", link.Type.Name, link.FormKey);
            if (masters.Contains(link.FormKey.ModKey))
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
                setup?.Invoke(duplicateRecord);
                log.Information(
                    "Cloned {Type} '{EditorId}' ({FormKey}) as {ClonedFormKey}",
                    link.Type.Name, sourceRecord.EditorID, link.FormKey, duplicateRecord.FormKey);
                return duplicateRecord.FormKey;
            }
        }

        private void ImportHeadPartDependencies(HeadPart headPart)
        {
            log.Debug(
                "Processing head part {FormKey} '{EditorId}' for cloning",
                headPart.FormKey, headPart.EditorID);
            headPart.TextureSet.SetTo(Import(headPart.TextureSet, mergedMod.TextureSets));
            if (headPart.Model != null && headPart.Model.AlternateTextures != null)
            {
                foreach (var altTexture in headPart.Model.AlternateTextures)
                    altTexture.NewTexture.SetTo(Import(altTexture.NewTexture, mergedMod.TextureSets));
            }
            var mergedExtraParts = headPart.ExtraParts
                .Select(x => Import(x, mergedMod.HeadParts, hp => ImportHeadPartDependencies(hp)))
                .NotNull()
                .ToList();
            headPart.ExtraParts.Clear();
            headPart.ExtraParts.AddRange(mergedExtraParts);
            log.Information(
                "Finished processing cloned head part {FormKey} '{EditorId}'",
                headPart.EditorID, headPart.FormKey);
        }
    }
}

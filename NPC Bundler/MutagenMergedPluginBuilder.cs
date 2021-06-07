using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class MutagenMergedPluginBuilder : IMergedPluginBuilder<FormKey>
    {
        public static readonly string MergeFileName = "NPC Appearances Merged.esp";

        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;
        private readonly ILogger log;

        public MutagenMergedPluginBuilder(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, ILogger log)
        {
            this.environment = environment;
            this.log = log.ForContext<MutagenMergedPluginBuilder>();
        }

        public MergedPluginResult Build(
            IReadOnlyList<NpcConfiguration<FormKey>> npcs, BuildSettings<FormKey> buildSettings,
            ProgressViewModel progress)
        {
            progress.StartStage("Backing up previous merge");
            var mergeFilePath = Path.Combine(environment.GameFolderPath, MergeFileName);
            if (File.Exists(mergeFilePath))
            {
                var backupPath = $"{mergeFilePath}.{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                log.Debug("Moved {mergeFilePath} to {backupPath}", mergeFilePath, backupPath);
                File.Move(mergeFilePath, backupPath, true);
                log.Information("Moved {mergeFilePath} to {backupPath}", mergeFilePath, backupPath);
            }

            progress.StartStage("Starting the merge");
            var mergedMod = new SkyrimMod(ModKey.FromNameAndExtension(MergeFileName), SkyrimRelease.SkyrimSE);
            var context = new MergeContext(environment, mergedMod, log);
            var result = new MergedPluginResult();

            var customizedNpcs = new List<Tuple<NpcConfiguration<FormKey>, Npc>>();
            progress.MaxProgress = customizedNpcs.Count;
            progress.AdjustRemaining(npcs.Count, 0.25f);
            progress.StartStage("Importing NPC defaults");
            foreach (var npc in npcs)
                using (LogContext.PushProperty("NPC", new { npc.Key, npc.EditorId, npc.Name }))
                using (LogContext.PushProperty("DefaultPluginName", npc.DefaultPluginName))
                {
                    log.Debug("" +
                        "Importing NPC defaults for {NpcLabel} from {DefaultPluginName}",
                        npc.DescriptiveLabel, npc.DefaultPluginName);
                    progress.CurrentProgress++;
                    if (!npc.HasCustomizations())
                    {
                        log.Debug("NPC has no customizations and will be skipped.");
                        continue;
                    }
                    log.Debug("Copying NPC defaults", npc.DefaultPluginName);
                    progress.ItemName = $"{npc.DescriptiveLabel}; Source: {npc.DefaultPluginName}";
                    var defaultModKey = ModKey.FromNameAndExtension(npc.DefaultPluginName);
                    var defaultNpc = environment.LoadOrder.GetModNpc(defaultModKey, npc.Key);
                    var mergedNpcRecord = mergedMod.Npcs.GetOrAddAsOverride(defaultNpc);
                    customizedNpcs.Add(Tuple.Create(npc, mergedNpcRecord));
                    context.AddMaster(npc.DefaultPluginName);
                    log.Information(
                        "Imported NPC defaults for {NpcLabel} from {DefaultPluginName}",
                        npc.DescriptiveLabel, npc.DefaultPluginName);
                }
            progress.JumpTo(0.25f);

            // TODO: Improve the ProgressViewModel so we don't need this stuff. Allow to define stages, stage weights,
            // and stage sizes, and have it do the rest automatically.
            float faceImportStageSize = buildSettings.EnableDewiggify ? 0.65f : 0.7f;
            progress.StartStage("Importing face overrides");
            progress.AdjustRemaining(customizedNpcs.Count, faceImportStageSize);
            foreach (var npcTuple in customizedNpcs)
            {
                var npc = npcTuple.Item1;
                var mergedNpcRecord = npcTuple.Item2;
                using (LogContext.PushProperty("NPC", new { npc.Key, npc.EditorId, npc.Name }))
                using (LogContext.PushProperty("FacePluginName", npc.FacePluginName))
                {
                    log.Debug(
                        "Importing NPC face attributes for {NpcLabel} from {FacePluginName}",
                        npc.DescriptiveLabel, npc.FacePluginName);
                    progress.ItemName = $"{npc.DescriptiveLabel}; Source: {npc.FacePluginName}";
                    var faceModKey = ModKey.FromNameAndExtension(npc.FacePluginName);
                    var faceMod = environment.LoadOrder.GetIfEnabled(faceModKey).Mod;
                    var faceNpcRecord = environment.LoadOrder.GetModNpc(faceModKey, npc.Key);
                    log.Debug("Importing shallow overrides", npc.FacePluginName);
                    // "Deep copy" doesn't copy dependencies, so we only do this for non-referential attributes.
                    mergedNpcRecord.DeepCopyIn(faceNpcRecord, new Npc.TranslationMask(defaultOn: false)
                    {
                        FaceMorph = true,
                        FaceParts = true,
                        HairColor = true,
                        TextureLighting = true,
                        TintLayers = true,
                    });
                    log.Debug("Importing head parts", npc.FacePluginName);
                    mergedNpcRecord.HeadParts.Clear();
                    foreach (var sourceHeadPart in faceNpcRecord.HeadParts)
                    {
                        var mergedHeadPart = context.Import(sourceHeadPart, mergedMod.HeadParts);
                        mergedNpcRecord.HeadParts.Add(mergedHeadPart.Value);
                    }
                    log.Debug("Importing face texture", npc.FacePluginName);
                    mergedNpcRecord.HeadTexture.SetTo(context.Import(faceNpcRecord.HeadTexture, mergedMod.TextureSets));
                    log.Information(
                        "Completed face import for {NpcLabel} from {FacePluginName}",
                        npc.DescriptiveLabel, npc.FacePluginName);
                    progress.CurrentProgress++;
                }
            }
            progress.JumpTo(0.25f + faceImportStageSize);

            var wigConversions = new List<NpcWigConversion>();
            if (buildSettings.EnableDewiggify)
            {
                progress.StartStage("Converting wigs");
                var wigs = customizedNpcs
                    .Where(x => x.Item1.FaceConfiguration.Wig != null)
                    .Select(x => x.Item1.FaceConfiguration.Wig);
                var wigMatches = buildSettings.WigResolver.ResolveAll(wigs)
                    .Where(x => x.HairKeys.Count > 0)
                    .ToDictionary(x => x.WigKey, x => x.HairKeys);
                var npcsWithMatchedWigs = customizedNpcs
                    .Where(x =>
                        x.Item1.FaceConfiguration.Wig != null &&
                        wigMatches.ContainsKey(x.Item1.FaceConfiguration.Wig.Key))
                    .ToList();
                progress.AdjustRemaining(npcsWithMatchedWigs.Count, 0.05f);
                foreach (var npcTuple in npcsWithMatchedWigs)
                {
                    var npc = npcTuple.Item1;
                    var mergedNpcRecord = npcTuple.Item2;
                    using (LogContext.PushProperty("NPC", new { npc.Key, npc.EditorId, npc.Name }))
                    using (LogContext.PushProperty("FacePluginName", npc.FacePluginName))
                    {
                        log.Debug(
                            "Converting wig for {NpcLabel} from {FacePluginName}: {WigKey}",
                            npc.DescriptiveLabel, npc.FacePluginName, npc.FaceConfiguration.Wig.Key);
                        progress.ItemName = $"{npc.DescriptiveLabel}; Source: {npc.FacePluginName}";
                        var hairKey = wigMatches[npc.FaceConfiguration.Wig.Key][0];
                        var oldHairParts = mergedNpcRecord.HeadParts
                            .Select(x => context.Resolve<IHeadPartGetter>(x.FormKey))
                            .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                            .Select(x => new { x.FormKey, x.EditorID, x.Model?.File })
                            .ToList();
                        mergedNpcRecord.HeadParts.Remove(oldHairParts.Select(x => x.FormKey));
                        var mergedHair = context.Import(hairKey.AsLink<IHeadPartGetter>(), mergedMod.HeadParts);
                        mergedNpcRecord.HeadParts.Add(mergedHair.Value);
                        log.Debug(
                            "Completed wig conversion for {NpcLabel} from {FacePluginName}: {WigKey}",
                            npc.DescriptiveLabel, npc.FacePluginName, npc.FaceConfiguration.Wig.Key);
                        wigConversions.Add(new NpcWigConversion
                        {
                            BasePluginName = npc.BasePluginName,
                            LocalFormIdHex = npc.LocalFormIdHex,
                            HairColor = GetHairColor(mergedNpcRecord),
                            AddedHeadParts = DescribeHeadParts(mergedHair.Value, context).ToList().AsReadOnly(),
                            RemovedHeadParts = oldHairParts
                                .SelectMany(x => DescribeHeadParts(x.FormKey, context))
                                .ToList()
                                .AsReadOnly(),
                        });
                        progress.CurrentProgress++;
                    }
                }
                progress.JumpTo(0.95f);
            }

            progress.StartStage("Saving");
            progress.JumpTo(0.99f);
            var outFilePath = Path.Combine(
                BundlerSettings.Default.ModRootDirectory, buildSettings.OutputModName, MergeFileName);
            mergedMod.WriteToBinaryParallel(outFilePath);

            progress.StartStage("Done");
            progress.CurrentProgress = progress.MaxProgress;

            return new MergedPluginResult
            {
                Meshes = mergedMod.HeadParts
                    .Where(x => x.Model != null)
                    .Select(x => x.Model.File.PrefixPath("meshes"))
                    .ToHashSet(),
                Morphs = mergedMod.HeadParts
                    .SelectMany(x => x.Parts)
                    .Select(x => x.FileName)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(x => x.PrefixPath("meshes"))
                    .ToHashSet(),
                Npcs = customizedNpcs.Select(x => x.Item1.EditorId).ToHashSet(),
                Textures = mergedMod.TextureSets
                    .SelectMany(x => new[]
                    {
                        x.Diffuse,
                        x.NormalOrGloss,
                        x.EnvironmentMaskOrSubsurfaceTint,
                        x.GlowOrDetailMap,
                        x.Height,
                        x.Environment,
                        x.Multilayer,
                        x.BacklightMaskOrSpecular,
                    })
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(x => x.PrefixPath("textures"))
                    .ToHashSet(),
                WigConversions = wigConversions.AsReadOnly(),
            };
        }

        private IEnumerable<HeadPartInfo> DescribeHeadParts(FormKey formKey, MergeContext context)
        {
            var headPart = context.Resolve<IHeadPartGetter>(formKey);
            return headPart.ExtraParts
                .SelectMany(x => DescribeHeadParts(x.FormKey, context))
                .Prepend(new HeadPartInfo {
                    EditorId = headPart.EditorID,
                    FileName = headPart.Model?.File?.PrefixPath("meshes")
                });
        }

        private Color? GetHairColor(INpcGetter npc)
        {
            if (npc.HairColor.IsNull)
                return null;
            var colorRecord = npc.HairColor.Resolve(environment.LinkCache);
            return colorRecord.Color;
        }

        class MergeContext
        {
            private readonly Dictionary<FormKey, IMajorRecord> duplicatedRecords = new();
            private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;
            private readonly ILogger log;
            private readonly HashSet<ModKey> masters = new();
            private readonly ISkyrimMod mergedMod;
            // We need to keep track of these ourselves, because merged records won't appear in the original LinkCache.
            private readonly Dictionary<FormKey, IMajorRecord> mergedRecords = new();

            public MergeContext(
                GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, ISkyrimMod mergedMod, ILogger log)
            {
                this.environment = environment;
                this.log = log;
                this.mergedMod = mergedMod;
            }

            public void AddMaster(string pluginName)
            {
                masters.Add(ModKey.FromNameAndExtension(pluginName));
            }

            public FormKey? Import<T, TGetter>(
                IFormLinkGetter<TGetter> link, IGroup<T> group)
                where T : SkyrimMajorRecord, TGetter
                where TGetter : class, ISkyrimMajorRecordGetter
            {
                Action<T> setup = null;
                // There's probably a switch expression that's better for this, but Mutagen doesn't make it obvious.
                if (typeof(IHeadPartGetter).IsAssignableFrom(link.Type))
                    setup = x => ImportHeadPartDependencies((HeadPart)(SkyrimMajorRecord)x);
                return Import(link, group, setup);
            }

            public T Resolve<T>(FormKey key) where T : class, IMajorRecordGetter
            {
                return GetIfMerged<T>(key) ?? key.AsLink<T>().Resolve(environment.LinkCache);
            }

            private T GetIfMerged<T>(FormKey key) where T : IMajorRecordGetter
            {
                return mergedRecords.TryGetValue(key, out var merged) ? (T)merged : default;
            }

            private FormKey? Import<T, TGetter>(
                IFormLinkGetter<TGetter> link, IGroup<T> group, Action<T> setup = null)
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
                var mergedExtraParts = headPart.ExtraParts
                    .Select(x => Import(x, mergedMod.HeadParts, hp => ImportHeadPartDependencies(hp)))
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToList();
                headPart.ExtraParts.Clear();
                headPart.ExtraParts.AddRange(mergedExtraParts);
                log.Information(
                    "Finished processing cloned head part {FormKey} '{EditorId}'",
                    headPart.EditorID, headPart.FormKey);
            }
        }
    }
}
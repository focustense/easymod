using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
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
            IReadOnlyList<NpcConfiguration<FormKey>> npcs, string outputModName, ProgressViewModel progress)
        {
            progress.StartStage("Backing up previous merge");
            var mergeFilePath = Path.Combine(environment.GameFolderPath, MergeFileName);
            if (File.Exists(mergeFilePath))
            {
                var backupPath = $"{mergeFilePath}.{DateTime.Now:yyyyMMdd_hhmmss}.bak";
                log.Debug("Moved {mergeFilePath} to {backupPath}", mergeFilePath, backupPath);
                File.Move(mergeFilePath, backupPath, true);
                log.Information("Moved {mergeFilePath} to {backupPath}", mergeFilePath, backupPath);
            }

            progress.StartStage("Starting the merge");
            var mergedMod = new SkyrimMod(ModKey.FromNameAndExtension(MergeFileName), SkyrimRelease.SkyrimSE);
            var result = new MergedPluginResult();

            var customizedNpcs = new List<Tuple<NpcConfiguration<FormKey>, Npc>>();
            var masters = new HashSet<ModKey>();
            progress.AdjustRemaining(npcs.Count, 0.25f);
            progress.MaxProgress = (int)Math.Floor(npcs.Count * 2 * 1.05);
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
                    masters.Add(ModKey.FromNameAndExtension(npc.DefaultPluginName));
                    log.Information(
                        "Imported NPC defaults for {NpcLabel} from {DefaultPluginName}",
                        npc.DescriptiveLabel, npc.DefaultPluginName);
                }
            progress.JumpTo(0.25f);

            // Merge-specific state, for tracking which records have been/still need to be duplicated.
            var duplicatedRecords = new Dictionary<FormKey, IMajorRecord>();

            FormKey? MakeStandalone<T, TGetter>(
                IFormLinkGetter<TGetter> link, IGroup<T> group, Action<T> setup = null)
                where T : SkyrimMajorRecord, TGetter
                where TGetter: class, ISkyrimMajorRecordGetter
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

            void MakeHeadPartStandalone(HeadPart headPart)
            {
                log.Debug(
                    "Processing head part {FormKey} '{EditorId}' for cloning",
                    headPart.FormKey, headPart.EditorID);
                headPart.TextureSet.SetTo(MakeStandalone(headPart.TextureSet, mergedMod.TextureSets));
                var mergedExtraParts = headPart.ExtraParts
                    .Select(x => MakeStandalone(x, mergedMod.HeadParts, hp => MakeHeadPartStandalone(hp)))
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToList();
                headPart.ExtraParts.Clear();
                headPart.ExtraParts.AddRange(mergedExtraParts);
                log.Information(
                    "Finished processing cloned head part {FormKey} '{EditorId}'",
                    headPart.EditorID, headPart.FormKey);
            }

            progress.StartStage("Importing face overrides");
            progress.AdjustRemaining(customizedNpcs.Count, 0.7f);
            progress.MaxProgress -= npcs.Count - customizedNpcs.Count;
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
                        var mergedHeadPart =
                            MakeStandalone(sourceHeadPart, mergedMod.HeadParts, hp => MakeHeadPartStandalone(hp));
                        mergedNpcRecord.HeadParts.Add(mergedHeadPart.Value);
                    }
                    log.Debug("Importing face texture", npc.FacePluginName);
                    mergedNpcRecord.HeadTexture.SetTo(MakeStandalone(faceNpcRecord.HeadTexture, mergedMod.TextureSets));
                    log.Information(
                        "Completed face import for {NpcLabel} from {FacePluginName}",
                        npc.DescriptiveLabel, npc.FacePluginName);
                    progress.CurrentProgress++;
                }
            }
            progress.JumpTo(0.95f);

            progress.StartStage("Saving");
            progress.JumpTo(0.99f);
            var outFilePath = Path.Combine(BundlerSettings.Default.ModRootDirectory, outputModName, MergeFileName);
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
            };
        }
    }
}
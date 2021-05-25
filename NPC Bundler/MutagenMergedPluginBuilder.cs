using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
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

        public MutagenMergedPluginBuilder(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment)
        {
            this.environment = environment;
        }

        public MergedPluginResult Build(
            IReadOnlyList<NpcConfiguration<FormKey>> npcs, string outputModName, ProgressViewModel progress)
        {
            progress.StartStage("Backing up previous merge");
            var mergeFilePath = Path.Combine(environment.GameFolderPath, MergeFileName);
            if (File.Exists(mergeFilePath))
                File.Move(mergeFilePath, $"{mergeFilePath}.{DateTime.Now:yyyyMMdd_hhmmss}.bak", true);

            progress.StartStage("Starting the merge");
            var mergedMod = new SkyrimMod(ModKey.FromNameAndExtension(MergeFileName), SkyrimRelease.SkyrimSE);

            var customizedNpcs = new List<Tuple<NpcConfiguration<FormKey>, Npc>>();
            var masters = new HashSet<ModKey>();
            progress.AdjustRemaining(npcs.Count, 0.25f);
            progress.MaxProgress = (int)Math.Floor(npcs.Count * 2 * 1.05);
            progress.StartStage("Importing NPC defaults");
            foreach (var npc in npcs)
            {
                progress.CurrentProgress++;
                if (!npc.HasCustomizations())
                    continue;
                progress.ItemName = $"{npc.DescriptiveLabel}; Source: {npc.DefaultPluginName}";
                var defaultModKey = ModKey.FromNameAndExtension(npc.DefaultPluginName);
                var defaultNpc = environment.LoadOrder.GetModNpc(defaultModKey, npc.Key);
                var mergedNpcRecord = mergedMod.Npcs.GetOrAddAsOverride(defaultNpc);
                customizedNpcs.Add(Tuple.Create(npc, mergedNpcRecord));
                masters.Add(ModKey.FromNameAndExtension(npc.DefaultPluginName));
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
                else if (masters.Contains(link.FormKey.ModKey))
                    return link.FormKey;
                else if (duplicatedRecords.TryGetValue(link.FormKey, out var cachedCopy))
                    return cachedCopy.FormKey;
                else
                {
                    var sourceRecord = link.Resolve(environment.LinkCache);
                    var duplicateRecord = group.AddNew();
                    duplicateRecord.DeepCopyIn(sourceRecord);
                    duplicatedRecords.Add(link.FormKey, duplicateRecord);
                    setup?.Invoke(duplicateRecord);
                    return duplicateRecord.FormKey;
                }
            }

            void MakeHeadPartStandalone(HeadPart headPart)
            {
                headPart.TextureSet.SetTo(MakeStandalone(headPart.TextureSet, mergedMod.TextureSets));
                var mergedExtraParts = headPart.ExtraParts
                    .Select(x => MakeStandalone(x, mergedMod.HeadParts, hp => MakeHeadPartStandalone(hp)))
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToList();
                headPart.ExtraParts.Clear();
                headPart.ExtraParts.AddRange(mergedExtraParts);
            }

            progress.StartStage("Importing face overrides");
            progress.AdjustRemaining(customizedNpcs.Count, 0.7f);
            progress.MaxProgress -= npcs.Count - customizedNpcs.Count;
            foreach (var npcTuple in customizedNpcs)
            {
                var npc = npcTuple.Item1;
                var mergedNpcRecord = npcTuple.Item2;
                progress.ItemName = $"{npc.DescriptiveLabel}; Source: {npc.FacePluginName}";
                var faceModKey = ModKey.FromNameAndExtension(npc.FacePluginName);
                var faceMod = environment.LoadOrder.GetIfEnabled(faceModKey).Mod;
                var faceNpcRecord = environment.LoadOrder.GetModNpc(faceModKey, npc.Key);
                // "Deep copy" doesn't copy dependencies, so we only do this for non-referential attributes.
                mergedNpcRecord.DeepCopyIn(faceNpcRecord, new Npc.TranslationMask(defaultOn: false)
                {
                    FaceMorph = true,
                    FaceParts = true,
                    HairColor = true,
                    TextureLighting = true,
                    TintLayers = true,
                });
                mergedNpcRecord.HeadParts.Clear();                    
                foreach (var sourceHeadPart in faceNpcRecord.HeadParts)
                {
                    var mergedHeadPart =
                        MakeStandalone(sourceHeadPart, mergedMod.HeadParts, hp => MakeHeadPartStandalone(hp));
                    mergedNpcRecord.HeadParts.Add(mergedHeadPart.Value);
                }
                mergedNpcRecord.HeadTexture.SetTo(MakeStandalone(faceNpcRecord.HeadTexture, mergedMod.TextureSets));
                progress.CurrentProgress++;
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
                Npcs = new HashSet<string>(npcs.Select(x => x.EditorId)),
            };
        }
    }
}
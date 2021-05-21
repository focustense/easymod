using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
    static class MergedPlugin
    {
        public static readonly string MergeFileName = "NPC Appearances Merged.esp";

        public static MergedPluginResult Build(
            IReadOnlyList<NpcConfiguration> npcs, string outputModName, ProgressViewModel progress)
        {

            progress.StartStage("Backing up previous merge");
            var dataPath = Meta.GetGlobal("DataPath");
            var mergeFilePath = Path.Combine(dataPath, MergeFileName);
            var outFilePath = Path.Combine(BundlerSettings.Default.ModRootDirectory, outputModName, MergeFileName);
            if (File.Exists(mergeFilePath))
                File.Move(mergeFilePath, $"{mergeFilePath}.{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.bak", true);

            // Unfortunately, concurrency is (probably?) not possible in this method due to the way in which XEditLib
            // passes data back to the caller, using a single block of shared memory to store the result of the last
            // operation - meaning, if one thread starts a new operation before the previous thread has a chance to
            // read the result of its own operation, it may get incorrect or corrupted data back.
            //
            // It might be possible to work around this with a lock, but seems probable that most threads would then
            // be spending >90% of their time in wait states, actually making the entire process slower than ST.
            //
            // So, at least until more tests can be done and/or some solution is found, this will be single-threaded.

            progress.StartStage("Starting the merge");
            using var g = new HandleGroup();
            var mergeFile = Files.FileByName(MergeFileName);
            if (mergeFile.Value == 0)
                mergeFile = Files.AddFile(MergeFileName);
            g.AddHandle(mergeFile);
            Files.NukeFile(mergeFile);

            // Key = FormID, value = Record handle
            var mergedNpcElementCache = new Dictionary<uint, Handle>();

            // Progress calculation is based on 2 iterations for each NPC plus 5% for final cleanup.
            progress.MaxProgress = (int)Math.Floor(npcs.Count * 2 * 1.05);
            progress.StartStage("Importing NPC defaults");

            // TODO: Currently each iteration creates another file handle by calling Files.FileByName. This isn't like
            // a native Windows "handle", but is there a possibility that it causes problems? If so, we can cache file
            // handles in a dictionary for this method so that we have at most one handle per file.
            foreach (var npc in npcs)
            {
                progress.ItemName = $"{FormatNpcLabel(npc)}; Source: {npc.DefaultPluginName}";
                progress.CurrentProgress++;

                if (!HasCustomizations(npc))
                    continue;

                var formIdHex = npc.FormId.ToString("X8");
                var defaultFile = g.AddHandle(Files.FileByName(npc.DefaultPluginName));
                var defaultNpcElement = g.AddHandle(Elements.GetElement(defaultFile, formIdHex));
                Masters.AddRequiredMasters(defaultNpcElement, mergeFile);
                var mergedNpcElement = g.AddHandle(Elements.CopyElement(defaultNpcElement, mergeFile));
                mergedNpcElementCache.Add(npc.FormId, mergedNpcElement);
            }

            // Faces can be tricky; some simple records can just be copied, but the real fun begins when we get to the
            // references - head parts, texture sets, etc. We are trying to create a merge (i.e. not require or want the
            // original NPC plugins), but we also don't want to just blindly deep-copy everything, since there will be
            // several references that are actually vanilla.
            //
            // MANY popular NPC mods (probably most) use Editor IDs that are unique, even across their various mods. But
            // some don't, which makes it probably unsafe to assume that two mods referencing a head part with the same
            // Editor ID are actually using the same part.
            //
            // A reasonable strategy seems to be to to deep-copy any head part that is not already in the merge file's
            // masters (so i.e. not vanilla), and reuse by file + editor ID or just global form ID. One consequence of
            // this is that we really need a second pass to do the faces, since the masters aren't fully known until
            // we've finished the first pass of default/non-head attributes.
            var importer = new ReferenceImporter(g, mergeFile);
            progress.StartStage("Importing face overrides");
            progress.MaxProgress -= npcs.Count - mergedNpcElementCache.Count;
            foreach (var npc in npcs)
            {
                if (!mergedNpcElementCache.TryGetValue(npc.FormId, out Handle mergedNpcElement))
                    continue;

                progress.ItemName = $"{FormatNpcLabel(npc)}; Source: {npc.FacePluginName}";
                progress.CurrentProgress++;

                Elements.RemoveElementIfExists(mergedNpcElement, "PNAM");
                Elements.RemoveElementIfExists(mergedNpcElement, "HCLF");
                Elements.RemoveElementIfExists(mergedNpcElement, "FTST");
                Elements.RemoveElementIfExists(mergedNpcElement, "NAMA");
                Elements.RemoveElementIfExists(mergedNpcElement, "NAM9");
                Elements.RemoveElementIfExists(mergedNpcElement, "QNAM");
                Elements.RemoveElementIfExists(mergedNpcElement, "TINC");
                Elements.RemoveElementIfExists(mergedNpcElement, "TINI");
                Elements.RemoveElementIfExists(mergedNpcElement, "TINV");

                var formIdHex = npc.FormId.ToString("X8");
                var faceFile = g.AddHandle(Files.FileByName(npc.FacePluginName));
                var faceNpcElement = g.AddHandle(Elements.GetElement(faceFile, formIdHex));

                // It turns out that xEdit's deep copy isn't exactly a deep copy. If the head part has any Extra Parts,
                // they'll be added as references to the original as a master, not deep-copied.
                // We'll clean masters at the end of the build, but for now, we have to manually replace the extra parts
                // with new copies in order to get rid of the master refs.
                void MakeHeadPartStandalone(Handle headPart)
                {
                    if (Elements.HasElement(headPart, "HNAM"))
                    {
                        var extraPartRefs = g.AddHandles(Elements.GetElements(headPart, "HNAM"));
                        var mergedExtraParts = extraPartRefs.Select(h =>
                        {
                            var part = importer.Import(h, out bool isNew);
                            return new { Handle = part, IsNew = isNew };
                        }).ToList();
                        Elements.RemoveElement(headPart, "HNAM");
                        foreach (var mergedExtraPart in mergedExtraParts)
                        {
                            Elements.AddArrayItem(
                                headPart, "HNAM", "",
                                ElementValues.GetUIntValue(mergedExtraPart.Handle).ToString("X8"));
                            if (mergedExtraPart.IsNew)
                                MakeHeadPartStandalone(mergedExtraPart.Handle);
                        }
                    }

                    // Texture set also doesn't get copied.
                    if (Elements.HasElement(headPart, "TNAM"))
                    {
                        var textureSetRef = g.AddHandle(Elements.GetElement(headPart, "TNAM"));
                        var mergedTextureSet = importer.Import(textureSetRef);
                        Elements.SetLinksTo(headPart, "TNAM", mergedTextureSet);
                    }
                }

                var headPartRefs = Elements.HasElement(faceNpcElement, "PNAM") ?
                    g.AddHandles(Elements.GetElements(faceNpcElement, "PNAM")) : Array.Empty<Handle>();
                foreach (var headPartRef in headPartRefs)
                {
                    var mergedHeadPart = importer.Import(headPartRef, out bool isNew);
                    var mergedHeadPartFormId = ElementValues.GetUIntValue(mergedHeadPart);
                    Elements.AddArrayItem(mergedNpcElement, "PNAM", "", mergedHeadPartFormId.ToString("X8"));

                    if (isNew)
                        MakeHeadPartStandalone(mergedHeadPart);

                    if (Elements.HasElement(faceNpcElement, "HCLF"))
                    {
                        var hairColorRef = g.AddHandle(Elements.GetElement(faceNpcElement, "HCLF"));
                        var mergedHairColor = importer.Import(hairColorRef);
                        g.AddHandle(Elements.AddElement(mergedNpcElement, "HCLF"));
                        Elements.SetLinksTo(mergedNpcElement, "HCLF", mergedHairColor);
                    }

                    if (Elements.HasElement(faceNpcElement, "FTST"))
                    {
                        var faceTextureSetRef = g.AddHandle(Elements.GetElement(faceNpcElement, "FTST"));
                        var mergedFaceTextureSet = importer.Import(faceTextureSetRef);
                        g.AddHandle(Elements.AddElement(mergedNpcElement, "FTST"));
                        Elements.SetLinksTo(mergedNpcElement, "FTST", mergedFaceTextureSet);
                    }

                    CopyIfExists(faceNpcElement, "NAMA", mergedNpcElement, g);
                    CopyIfExists(faceNpcElement, "NAM9", mergedNpcElement, g);
                    CopyIfExists(faceNpcElement, "QNAM", mergedNpcElement, g);
                    CopyIfExists(faceNpcElement, "Tint Layers", mergedNpcElement, g);
                }
            }

            progress.StartStage("Cleaning up");
            progress.CurrentProgress = (int)Math.Round(progress.MaxProgress * 0.95);
            Masters.CleanMasters(mergeFile);

            progress.StartStage("Building resource list");
            progress.CurrentProgress = (int)Math.Round(progress.MaxProgress * 0.98);
            var result = GetResult(mergeFile, g);

            progress.StartStage("Saving");
            progress.CurrentProgress = (int)Math.Floor(progress.MaxProgress * 0.99);
            // This doesn't save the file we expect - it will actually have an ".esp.save" extension.
            Files.SaveFile(mergeFile);
            File.Move($"{mergeFilePath}.save", outFilePath, true);

            progress.StartStage("Done");
            progress.CurrentProgress = progress.MaxProgress;

            return result;
        }

        private static void CopyIfExists(Handle srcElement, string path, Handle dstElement, HandleGroup g)
        {
            if (Elements.HasElement(srcElement, path))
            {
                var elementToCopy = g.AddHandle(Elements.GetElement(srcElement, path));
                Elements.CopyElement(elementToCopy, dstElement);
            }
        }

        private static string FormatNpcLabel(NpcConfiguration npc)
        {
            return $"'{npc.Name}' ({npc.BasePluginName} - {npc.EditorId})";
        }

        private static MergedPluginResult GetResult(Handle mergeFile, HandleGroup g)
        {
            // We COULD pull all of this information as the patch is being generated, and it would probably be a little
            // faster; this way is cleaner and makes it easier to see where the data comes from.
            var result = new MergedPluginResult();

            foreach (var npc in g.AddHandles(Elements.GetElements(mergeFile, "NPC_")))
                result.Npcs.Add(RecordValues.GetEditorId(npc));

            foreach (var headPart in g.AddHandles(Elements.GetElements(mergeFile, "HDPT")))
            {
                result.Meshes.Add(ElementValues.GetValue(headPart, "Model\\MODL").PrefixPath("meshes"));
                // There's also MODS - alternate textures for model - but precisely zero of the 30 or so test mods used
                // this. If any are found, support should be added.

                var parts = Elements.HasElement(headPart, "Parts") ?
                    g.AddHandles(Elements.GetElements(headPart, "Parts")) : Array.Empty<Handle>();
                foreach (var part in parts)
                    result.Morphs.Add(ElementValues.GetValue(part, "NAM1").PrefixPath("meshes"));
            }

            foreach (var textureSet in g.AddHandles(Elements.GetElements(mergeFile, "TXST")))
            {
                var textures = g.AddHandle(Elements.GetElement(textureSet, "Textures (RGB/A)"));
                var textureFiles = Enumerable.Range(0, 8)
                    .Select(i => ElementValues.GetValue(textures, $"TX0{i}"))
                    .Where(s => !string.IsNullOrEmpty(s));
                foreach (var textureFile in textureFiles)
                    result.Textures.Add(textureFile.PrefixPath("textures"));
            }

            // NPC_ records will reference head parts and texture sets, but don't themselves contain any direct
            // references to file names.
            return result;
        }

        private static bool HasCustomizations(NpcConfiguration npc)
        {
            return
                (npc.DefaultPluginName != npc.BasePluginName || npc.FacePluginName != npc.DefaultPluginName) &&
                (!FileStructure.IsDlc(npc.DefaultPluginName) || !FileStructure.IsDlc(npc.FacePluginName));
        }
    }

    class ReferenceImporter
    {
        private readonly Dictionary<string, Handle> cache = new();
        private readonly Handle destFile;
        private readonly HandleGroup g;
        private readonly HashSet<string> masters;

        public ReferenceImporter(HandleGroup g, Handle destFile)
        {
            this.g = g;
            this.destFile = destFile;
            masters = new(Masters.GetMasterNames(destFile));
        }

        public Handle Import(Handle refHandle)
        {
            return Import(refHandle, out bool _);
        }

        public Handle Import(Handle refHandle, out bool isNew)
        {
            // If we use GetUIntValue here, XEditLib gives us a "quasi-local" form ID, which has a leading bte for the
            // load order, but the order is actually an index into that plugin's masters, not the full load order.
            // On the other hand, the string result produced by GetValue includes a bunch of stuff including the editor
            // ID and the true load order index, which should be unique enough for our purposes here.
            var recordKey = ElementValues.GetValue(refHandle);
            isNew = false;
            if (!cache.TryGetValue(recordKey, out Handle importedElement))
            {
                var sourceElement = ResolveLink(refHandle, out var sourceFile);
                if (masters.Contains(sourceFile))
                    importedElement = sourceElement;
                else
                {
                    Masters.AddRequiredMasters(sourceElement, destFile, true);
                    var mergedElement = g.AddHandle(Elements.CopyElement(sourceElement, destFile, true));
                    importedElement = mergedElement;
                    isNew = true;
                }
                cache.Add(recordKey, importedElement);
            }
            return importedElement;
        }

        private Handle ResolveLink(Handle link, out string fileName)
        {
            var master = g.AddHandle(Elements.GetLinksTo(link, ""));
            var winningOverride = g.AddHandle(Records.GetWinningOverride(master));
            fileName = FileValues.GetFileName(g.AddHandle(Elements.GetElementFile(winningOverride)));
            return winningOverride;
        }
    }

    public class MergedPluginResult
    {
        public ISet<string> Meshes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Morphs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Npcs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Textures { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
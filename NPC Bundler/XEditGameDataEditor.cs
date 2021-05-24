using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditGameDataEditor : IGameDataEditor<uint>
    {
        private readonly IArchiveProvider archiveProvider = new XEditArchiveProvider();
        private readonly IModPluginMapFactory modPluginMapFactory = new XEditModPluginMapFactory();

        public IArchiveProvider ArchiveProvider => archiveProvider;
        public IModPluginMapFactory ModPluginMapFactory => modPluginMapFactory;

        public XEditGameDataEditor()
        {
            Meta.Initialize();
            Setup.SetGameMode(Setup.GameMode.SSE);
        }

        public IEnumerable<string> GetAvailablePlugins()
        {
            return Setup.GetActivePlugins().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public IEnumerable<string> GetLoadedPlugins()
        {
            return Setup.GetLoadedFileNames();
        }

        public int GetLoadOrderIndex(string pluginName)
        {
            using var g = new HandleGroup();
            var file = g.AddHandle(Files.FileByName(pluginName));
            return Files.GetFileLoadOrder(file);
        }

        public bool IsMaster(string pluginName)
        {
            using var g = new HandleGroup();
            var file = g.AddHandle(Files.FileByName(pluginName));
            var header = g.AddHandle(Files.GetFileHeader(file));
            return ElementValues.GetFlag(header, @"Record Header\Record Flags", "ESM");
        }

        public async Task Load(IEnumerable<string> pluginNames)
        {
            var loadOrder = string.Join('\n', pluginNames);
            Setup.LoadPlugins(loadOrder, true);
            await WaitForLoad().ConfigureAwait(true);
        }

        public void ReadNpcRecords(string pluginName, IDictionary<uint, IMutableNpc<uint>> cache)
        {
            using var g = new HandleGroup();
            var file = g.AddHandle(Files.FileByName(pluginName));
            var npcRecords = g.AddHandles(Records.GetRecords(file, "NPC_", true));
            foreach (var npcRecord in npcRecords)
            {
                var formId = Records.GetFormId(npcRecord, false, false);
                if (Records.IsOverride(npcRecord))
                {
                    var npc = cache[formId];
                    var faceOverrides =
                        GetFaceOverrides(file, npcRecord, out bool affectsFaceGen, out var itpoFileName);
                    npc.AddOverride(new NpcOverride<uint>(pluginName, faceOverrides, affectsFaceGen, itpoFileName));
                }
                else
                {
                    cache.Add(formId, new NpcInfo<uint>
                    {
                        BasePluginName = pluginName,
                        EditorId = RecordValues.GetEditorId(npcRecord),
                        Key = Records.GetFormId(npcRecord, false, false),
                        LocalFormIdHex = Records.GetHexFormId(npcRecord, false, true),
                        // Checking for FULL isn't totally necessary, but avoids spamming log warnings.
                        Name = Elements.HasElement(npcRecord, "FULL") ?
                            ElementValues.GetValue(npcRecord, "FULL") : "",
                    });
                }
            }
        }

        private static NpcFaceData<uint> GetFaceOverrides(
            Handle file, Handle npcRecord, out bool affectsFaceGen, out string itpoPluginName)
        {
            itpoPluginName = null;
            if (Records.IsMaster(npcRecord))
            {
                affectsFaceGen = false;
                return null;
            }

            using var g = new HandleGroup();
            var previousRecord = g.AddHandle(Records.GetPreviousOverride(npcRecord, file));
            if (previousRecord.Value != 0 && Records.IsItpo(npcRecord))
            {
                var itpoFile = g.AddHandle(Elements.GetElementFile(previousRecord));
                itpoPluginName = FileValues.GetFileName(itpoFile);
            }
            else
                previousRecord = g.AddHandle(Records.GetMasterRecord(npcRecord));
            var overrideFaceData = ReadFaceData(npcRecord, g);
            var overrideRace = GetFormId(npcRecord, "RNAM", g);
            var previousFaceData = ReadFaceData(previousRecord, g);
            var previousRace = GetFormId(previousRecord, "RNAM", g);
            affectsFaceGen = !NpcFaceData.EqualsForFaceGen(overrideFaceData, previousFaceData);
            return (!NpcFaceData.Equals(overrideFaceData, previousFaceData) || (overrideRace != previousRace)) ?
                overrideFaceData : null;
        }

        private static uint? GetFormId(Handle npcRecord, string path, HandleGroup g)
        {
            return Elements.HasElement(npcRecord, path) ?
                ElementValues.GetUIntValue(g.AddHandle(Elements.GetElement(npcRecord, path))) : null;
        }

        private static uint[] GetFormIds(Handle npcRecord, string path, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, path))
                return Array.Empty<uint>();
            return g.AddHandles(Elements.GetElements(npcRecord, path, true))
                .Select(handle => ElementValues.GetUIntValue(handle))
                .ToArray();
        }

        private static NpcFaceData<uint> ReadFaceData(Handle npcRecord, HandleGroup g)
        {
            var headPartIds = GetFormIds(npcRecord, "PNAM", g);
            var hairColorId = GetFormId(npcRecord, "HCLF", g);
            var faceTextureSetId = GetFormId(npcRecord, "FTST", g);
            var skinTone = ReadSkinTone(npcRecord, g);
            var faceMorphs = ReadFaceMorphs(npcRecord, g);
            var faceParts = ReadFaceParts(npcRecord, g);
            var faceTints = ReadFaceTints(npcRecord, g);
            return new NpcFaceData<uint>(
                headPartIds, hairColorId, faceTextureSetId, skinTone, faceMorphs, faceParts, faceTints);
        }

        private static NpcFaceMorphs ReadFaceMorphs(Handle npcRecord, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, "NAM9"))
                return null;
            var faceMorphs = g.AddHandle(Elements.GetElement(npcRecord, "NAM9"));
            return new NpcFaceMorphs(
                ElementValues.GetFloatValue(faceMorphs, "Nose Long/Short"),
                ElementValues.GetFloatValue(faceMorphs, "Nose Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Jaw Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Jaw Narrow/Wide"),
                ElementValues.GetFloatValue(faceMorphs, "Jaw Farward/Back"),
                ElementValues.GetFloatValue(faceMorphs, "Cheeks Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Cheeks Farward/Back"),
                ElementValues.GetFloatValue(faceMorphs, "Eyes Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Eyes In/Out"),
                ElementValues.GetFloatValue(faceMorphs, "Brows Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Brows In/Out"),
                ElementValues.GetFloatValue(faceMorphs, "Brows Farward/Back"),
                ElementValues.GetFloatValue(faceMorphs, "Lips Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Lips In/Out"),
                ElementValues.GetFloatValue(faceMorphs, "Chin Narrow/Wide"),
                ElementValues.GetFloatValue(faceMorphs, "Chin Up/Down"),
                ElementValues.GetFloatValue(faceMorphs, "Chin Underbite/Overbite"),
                ElementValues.GetFloatValue(faceMorphs, "Eyes Farward/Back"));
        }

        private static NpcFaceParts ReadFaceParts(Handle npcRecord, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, "NAMA"))
                return null;
            var faceParts = g.AddHandle(Elements.GetElement(npcRecord, "NAMA"));
            return new NpcFaceParts(
                // We seem to get some weird overflows in plugins like USSEP that can specify crazy
                // values like 4294967295, since the underlying xEdit code appears to be implemented
                // using signed ints. Reading floats seems to be a reliable workaround, since the
                // underlying Variant *is* read correctly, it's just the explicit cast that's wrong.
                (uint)ElementValues.GetFloatValue(faceParts, "Nose"),
                (uint)ElementValues.GetFloatValue(faceParts, "Eyes"),
                (uint)ElementValues.GetFloatValue(faceParts, "Mouth"));
        }

        private static NpcFaceTint[] ReadFaceTints(Handle npcRecord, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, "Tint Layers"))
                return Array.Empty<NpcFaceTint>();
            var tintLayers = g.AddHandles(Elements.GetElements(npcRecord, "Tint Layers"));
            return tintLayers
                .Select(tintLayer =>
                {
                    var tintColor = g.AddHandle(Elements.GetElement(tintLayer, "TINC"));
                    return new NpcFaceTint(
                        ElementValues.GetUIntValue(tintLayer, "TINI"),
                        new NpcFaceTintColor(
                            ElementValues.GetUIntValue(tintColor, "Red"),
                            ElementValues.GetUIntValue(tintColor, "Green"),
                            ElementValues.GetUIntValue(tintColor, "Blue"),
                            ElementValues.GetUIntValue(tintColor, "Alpha")),
                        ElementValues.GetUIntValue(tintLayer, "TINV"));
                }).ToArray();
        }

        private static NpcSkinTone ReadSkinTone(Handle npcRecord, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, "QNAM"))
                return null;
            var skinTone = g.AddHandle(Elements.GetElement(npcRecord, "QNAM"));
            return new NpcSkinTone(
                ElementValues.GetUIntValue(skinTone, "Red"),
                ElementValues.GetUIntValue(skinTone, "Green"),
                ElementValues.GetUIntValue(skinTone, "Blue"));
        }

        private static async Task WaitForLoad()
        {
            Setup.LoaderState loaderStatus;
            do
            {
                loaderStatus = Setup.GetLoaderStatus();
                if (loaderStatus != Setup.LoaderState.IsActive)
                    break;
                await Task.Delay(50);
            } while (true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
#nullable enable
#pragma warning disable IDE1006 // Naming Styles
    public interface Npc
#pragma warning restore IDE1006 // Naming Styles
    {
        string BasePluginName { get; }
        string EditorId { get; }
        uint FormId { get; }
        string LocalFormIdHex { get; }
        string Name { get; }
        IReadOnlyList<NpcOverride> Overrides { get; }

        public static NpcFaceData? GetFaceOverrides(
            Handle npcRecord, Handle file, out bool affectsFaceGen, out string? itpoFileName)
        {
            itpoFileName = null;
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
                itpoFileName = FileValues.GetFileName(itpoFile);
            }
            else
                previousRecord = g.AddHandle(Records.GetMasterRecord(npcRecord));
            var overrideFaceData = ReadFaceData(npcRecord, g);
            var masterFaceData = ReadFaceData(previousRecord, g);
            affectsFaceGen = !FaceGenDataEquals(overrideFaceData, masterFaceData);
            return !FaceDataEquals(overrideFaceData, masterFaceData) ? overrideFaceData : null;
        }

        private static bool FaceDataEquals(NpcFaceData a, NpcFaceData b)
        {
            return FaceGenDataEquals(a, b) &&
                a.FaceTextureSetId == b.FaceTextureSetId &&
                a.HairColorId == b.HairColorId &&
                a.SkinTone == b.SkinTone;
        }

        private static bool FaceGenDataEquals(NpcFaceData a, NpcFaceData b)
        {
            return
                // Edits to Face Texture Set, Face Tints and Hair Color do not seem to trigger facegen conflicts.
                // Those are ignored here.
                a.HeadPartIds.SequenceEqual(b.HeadPartIds) &&
                a.FaceMorphs == b.FaceMorphs &&
                a.FaceParts == b.FaceParts &&
                a.FaceTints.SequenceEqual(b.FaceTints);
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

        private static NpcFaceData ReadFaceData(Handle npcRecord, HandleGroup g)
        {
            var headPartIds = GetFormIds(npcRecord, "PNAM", g);
            var hairColorId = GetFormId(npcRecord, "HCLF", g);
            var faceTextureSetId = GetFormId(npcRecord, "FTST", g);
            var skinTone = ReadSkinTone(npcRecord, g);
            var faceMorphs = ReadFaceMorphs(npcRecord, g);
            var faceParts = ReadFaceParts(npcRecord, g);
            var faceTints = ReadFaceTints(npcRecord, g);
            return new NpcFaceData(
                headPartIds, hairColorId, faceTextureSetId, skinTone, faceMorphs, faceParts, faceTints);
        }

        private static NpcFaceMorphs? ReadFaceMorphs(Handle npcRecord, HandleGroup g)
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

        private static NpcFaceParts? ReadFaceParts(Handle npcRecord, HandleGroup g)
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

        private static NpcSkinTone? ReadSkinTone(Handle npcRecord, HandleGroup g)
        {
            if (!Elements.HasElement(npcRecord, "QNAM"))
                return null;
            var skinTone = g.AddHandle(Elements.GetElement(npcRecord, "QNAM"));
            return new NpcSkinTone(
                ElementValues.GetUIntValue(skinTone, "Red"),
                ElementValues.GetUIntValue(skinTone, "Green"),
                ElementValues.GetUIntValue(skinTone, "Blue"));
        }
    }

    public record NpcOverride(string PluginName, NpcFaceData? FaceData, bool AffectsFaceGen, string? ItpoPluginName)
    {
        public bool HasFaceOverride => FaceData != null;
    }

    public record NpcFaceData(
            uint[] HeadPartIds, uint? HairColorId, uint? FaceTextureSetId, NpcSkinTone? SkinTone,
            NpcFaceMorphs? FaceMorphs, NpcFaceParts? FaceParts, NpcFaceTint[] FaceTints);

    public record NpcFaceMorphs(
        double NoseLongShort, double NoseUpDown, double JawUpDown, double JawNarrowWide, double JawForwardBack,
        double CheeksUpDown, double CheeksForwardBack, double EyesUpDown, double EyesInOut, double BrowsUpDown,
        double BrowsInOut, double BrowsForwardBack, double LipsUpDown, double LipsInOut, double ChinThinWide,
        double ChinUpDown, double ChinUnderbiteOverbite, double EyesForwardBack); // Ignore the "unknown" value.

    // Excludes the "unknown" value.
    public record NpcFaceParts(uint Nose, uint Eyes, uint Mouth);

    // TIAS is labeled as "preset" which seems to have no effect; ignore.
    public record NpcFaceTint(uint Layer, NpcFaceTintColor Color, uint Value);

    public record NpcFaceTintColor(uint Red, uint Green, uint Blue, uint Alpha);

    public record NpcSkinTone(uint Red, uint Green, uint Blue);
#nullable restore
}

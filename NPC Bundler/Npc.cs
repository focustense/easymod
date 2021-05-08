using System;
using System.Collections.Generic;
using System.Linq;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
#nullable enable
    public interface Npc
    {
        string BasePluginName { get; }
        uint FormId { get; }
        string LocalFormIdHex { get; }
        string Name { get; }
        IReadOnlyList<NpcOverride> Overrides { get; }

        public static NpcFaceData? GetFaceOverrides(Handle npcRecord)
        {
            using var g = new HandleGroup();
            var overrideFaceData = ReadFaceData(npcRecord, g);
            var masterRecord = g.AddHandle(Records.GetMasterRecord(npcRecord));
            var masterFaceData = ReadFaceData(masterRecord, g);
            return overrideFaceData != masterFaceData ? overrideFaceData : null;
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
                ElementValues.GetIntValue(faceParts, "Nose"),
                ElementValues.GetIntValue(faceParts, "Eyes"),
                ElementValues.GetIntValue(faceParts, "Mouth"));
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
    }

    public record NpcOverride(string PluginName, NpcFaceData? faceData);

    public record NpcFaceData(
            uint[] HeadPartIds, uint? HairColorId, uint? FaceTextureSetId, NpcSkinTone? SkinTone,
            NpcFaceMorphs? FaceMorphs, NpcFaceParts? FaceParts, NpcFaceTint[] FaceTints);

    public record NpcFaceMorphs(
        double NoseLongShort, double NoseUpDown, double JawUpDown, double JawNarrowWide, double JawForwardBack,
        double CheeksUpDown, double CheeksForwardBack, double EyesUpDown, double EyesInOut, double BrowsUpDown,
        double BrowsInOut, double BrowsForwardBack, double LipsUpDown, double LipsInOut, double ChinThinWide,
        double ChinUpDown, double ChinUnderbiteOverbite, double EyesForwardBack); // Ignore the "unknown" value.

    // Excludes the "unknown" value.
    public record NpcFaceParts(int Nose, int Eyes, int Mouth);

    // TIAS is labeled as "preset" which seems to have no effect; ignore.
    public record NpcFaceTint(uint Layer, NpcFaceTintColor Color, uint Value);

    public record NpcFaceTintColor(uint Red, uint Green, uint Blue, uint Alpha);

    // TODO: Does skin tone or "texture lighting" actually affect the face?
    public record NpcSkinTone(uint Red, uint Green, uint Blue);
#nullable restore
}

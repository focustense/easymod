﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NPC_Bundler
{
    public interface INpc<TKey>
        where TKey : struct
    {
        string BasePluginName { get; }
        string EditorId { get; }
        bool IsFemale { get; }
        TKey Key { get; }
        string LocalFormIdHex { get; }
        string Name { get; }
        IReadOnlyList<NpcOverride<TKey>> Overrides { get; }
    }

    public interface IMutableNpc<TKey> : INpc<TKey>
        where TKey : struct
    {
        void AddOverride(NpcOverride<TKey> overrideInfo);
    }

    public record NpcOverride<TKey>(
        string PluginName, NpcFaceData<TKey> FaceData, bool AffectsFaceGen, string ItpoPluginName)
        where TKey : struct
    {
        public bool HasFaceOverride => FaceData != null;
    }

    public static class NpcFaceData
    {
        public static bool Equals<TKey>(NpcFaceData<TKey> a, NpcFaceData<TKey> b)
            where TKey : struct
        {
            return EqualsForFaceGen(a, b) &&
                Equals(a.FaceTextureSetId, b.FaceTextureSetId) &&
                Equals(a.HairColorId, b.HairColorId) &&
                a.SkinTone == b.SkinTone;
        }

        public static bool EqualsForFaceGen<TKey>(NpcFaceData<TKey> a, NpcFaceData<TKey> b)
            where TKey : struct
        {
            return
                // Edits to Face Texture Set, Face Tints and Hair Color do not seem to trigger facegen conflicts.
                // Those are ignored here.
                a.HeadPartIds.SequenceEqual(b.HeadPartIds) &&
                a.FaceMorphs == b.FaceMorphs &&
                a.FaceParts == b.FaceParts &&
                a.FaceTints.SequenceEqual(b.FaceTints);
        }
    }

    public record NpcFaceData<TKey>(
            TKey[] HeadPartIds, TKey? HairColorId, TKey? FaceTextureSetId, NpcSkinTone SkinTone,
            NpcFaceMorphs FaceMorphs, NpcFaceParts FaceParts, NpcFaceTint[] FaceTints)
        where TKey : struct
    {
        public NpcFaceData() : this(Array.Empty<TKey>(), null, null, null, null, null, Array.Empty<NpcFaceTint>()) { }
    }

    public record NpcFaceMorphs(
        double NoseLongShort, double NoseUpDown, double JawUpDown, double JawNarrowWide, double JawForwardBack,
        double CheeksUpDown, double CheeksForwardBack, double EyesUpDown, double EyesInOut, double BrowsUpDown,
        double BrowsInOut, double BrowsForwardBack, double LipsUpDown, double LipsInOut, double ChinThinWide,
        double ChinUpDown, double ChinUnderbiteOverbite, double EyesForwardBack) // Ignore the "unknown" value
    {
        public NpcFaceMorphs() : this(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) { }
    }

    // Excludes the "unknown" value.
    public record NpcFaceParts(uint Nose, uint Eyes, uint Mouth);

    // TIAS is labeled as "preset" which seems to have no effect; ignore.
    public record NpcFaceTint(uint Layer, NpcFaceTintColor Color, float Value);

    public record NpcFaceTintColor(uint Red, uint Green, uint Blue, uint Alpha);

    public record NpcSkinTone(uint Red, uint Green, uint Blue);
}

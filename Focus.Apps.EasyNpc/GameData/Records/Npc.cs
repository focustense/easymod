using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.GameData.Records
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

    public class NpcOverride<TKey>
        where TKey : struct
    {
        public NpcFaceData<TKey> FaceData { get; init; }
        public bool FaceOverridesAffectFaceGen { get; init; }
        public bool ModifiesBehavior { get; init; }
        public bool ModifiesBody { get; init; }
        public bool ModifiesFace => FaceData != null;
        public bool ModifiesOutfits { get; init; }
        public string ItpoPluginName { get; init; }
        public string PluginName { get; private init; }
        public NpcWigInfo<TKey> Wig { get; init; }

        public NpcOverride(string pluginName)
        {
            PluginName = pluginName;
        }
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

    // Baldness heuristic only matters for safety checks. If we fail to de-wiggify a character, and their wig is simply
    // covering up an actual hair headpart, then they'll still end up with some kind of hair when we don't carry over
    // the Worn Armor. However, if they're bald underneath the wig, then they'll be just plain bald in the merge, and we
    // don't want to give the user a bunch of bald female NPCs - at least, not without warning them.
    public record NpcWigInfo<TKey>(TKey Key, string ModelName, bool IsBald);
}

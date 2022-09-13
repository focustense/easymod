using System.Text.Json;

namespace Focus.Tools.EasyFollower
{
    public class RaceMenuPreset
    {
        // jslot files have many more fields, but these are the only ones we're interested in.
        public ActorInfo Actor { get; set; } = new();
        public List<HeadPartReference> HeadParts { get; set; } = new();
        public MorphSettings Morphs { get; set; } = new();
        public List<Override> Overrides { get; set; } = new();
    }

    public class ActorInfo
    {
        public int HairColor { get; set; }
        public string HeadTexture { get; set; } = "";
        public float Weight { get; set; }
    }

    // "Default" in the sense that they refer to vanilla capabilities, i.e. no custom morphs
    public class DefaultMorphSettings
    {
        public List<float> Morphs { get; set; } = new();
        public List<long> Presets { get; set; } = new(); // Probably don't need these
    }

    public class HeadPartReference
    {
        public long FormId { get; set; } // Load-order dependent
        public string FormIdentifier { get; set; } = ""; // "Plugin.esp|xxxxxx" format
        public int Type { get; set; } // Does not match ESP format, probably useless
    }

    public class MorphSettings
    {
        public DefaultMorphSettings Default { get; set; } = new();
    }

    // Overrides refer to NiOverride, i.e. face paints, body paints, etc.
    public class Override
    {
        public string Node { get; set; } = "";
        public List<OverrideValue> Values { get; set; } = new();
    }

    // NiOverride exposes functions like AddNodeOverrideFloat or AddNodeOverrideString which all
    // accept a node name (in jslot, this is in the parent Override), key, index and value. Type
    // appears to be something RaceMenu uses internally to keep track of the node names.
    public class OverrideValue
    {
        public JsonElement Data { get; set; } = new(); // Data type depends on the key and index
        public int Index { get; set; }
        public int Key { get; set; }
        public int Type { get; set; } // Likely should not use this, per comments above.
    }
}

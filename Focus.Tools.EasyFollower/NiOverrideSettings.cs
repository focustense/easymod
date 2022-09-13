using System.Drawing;
using System.Text.Json;

namespace Focus.Tools.EasyFollower
{
    class NiNodeOverrideSettings
    {
        enum Key
        {
            EmissiveColor = 0,
            TintColor = 7,
            TexturePath = 9,
        }

        public static IEnumerable<NiNodeOverrideSettings> FromRaceMenuPreset(
            IEnumerable<Override> overrides)
        {
            return overrides
                .Select(x =>
                {
                    var textureValue = PickValue(x.Values, Key.TexturePath);
                    var texturePath = textureValue?.Data.GetString();
                    if (string.IsNullOrWhiteSpace(texturePath))
                        return null;
                    // Alpha values are already embedded in the color values - they are ARGB.
                    // So we don't need to separately pick the tint/emissive alpha values.
                    var tintColorValue = PickValue(x.Values, Key.TintColor, -1);
                    if (tintColorValue?.Data.TryGetInt32(out var tintColorArgb) != true)
                        tintColorArgb = 0;
                    var emissiveColorValue = PickValue(x.Values, Key.EmissiveColor, -1);
                    if (emissiveColorValue?.Data.TryGetInt32(out var emissiveColorArgb) != true)
                        emissiveColorArgb = 0;
                    return (tintColorArgb != 0 || emissiveColorArgb != 0)
                        ? new NiNodeOverrideSettings
                        {
                            NodeName = x.Node,
                            TextureIndex = textureValue!.Index,
                            TexturePath = texturePath,
                            TintColor = Color.FromArgb(tintColorArgb),
                            EmissiveColor = Color.FromArgb(emissiveColorArgb),
                        }
                        : null;
                })
                .NotNull();
        }

        private static OverrideValue? PickValue(
            IEnumerable<OverrideValue> overrideValues, Key key, int? index = null)
        {
            var candidates = overrideValues.Where(x => x.Key == (int)key);
            if (index != null)
                candidates = candidates.Where(x => x.Index == index);
            return candidates.FirstOrDefault();
        }

        public string NodeName { get; init; } = "";
        public int TextureIndex { get; init; }
        public string TexturePath { get; init; } = "";
        public Color TintColor { get; init; }
        public Color EmissiveColor { get; init; }
    }
}

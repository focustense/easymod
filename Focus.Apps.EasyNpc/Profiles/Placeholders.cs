using Focus.ModManagers;

namespace Focus.Apps.EasyNpc.Profiles
{
    public static class Placeholders
    {
        private static readonly string BaseGameLabel = "Vanilla";

        public static readonly ModComponentInfo BaseGameComponent = new(
            new ModLocatorKey(string.Empty, BaseGameLabel), string.Empty, BaseGameLabel, string.Empty);

        public static readonly ModInfo BaseGameMod = new(string.Empty, BaseGameLabel)
        {
            Components = new[] { BaseGameComponent },
        };
    }
}

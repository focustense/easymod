using System;

namespace Focus.Apps.EasyNpc.Main
{
    public class StartupWarning
    {
        public string Title { get; init; }
        public object Description { get; init; }
    }

    public class StartupWarnings
    {
        public static readonly StartupWarning MissingVortexManifest = new()
        {
            Title = "Unsupported vortex launch",
            Description = new MissingVortexManifestContent(),
        };
    }

    public class MissingVortexManifestContent
    {
        public string ExtensionUrl { get; private init; } = "https://www.nexusmods.com/site/mods/265";
    }
}
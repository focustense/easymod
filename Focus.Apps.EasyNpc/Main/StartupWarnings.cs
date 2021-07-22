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

        public static StartupWarning UnsupportedGame(string gameId, string gameName) => new()
        {
            Title = "Unsupported game instance",
            Description = new UnsupportedGameContent(gameId, gameName),
        };
    }

    public class MissingVortexManifestContent
    {
        public string ExtensionUrl { get; private init; } = "https://www.nexusmods.com/site/mods/265";
    }

    public class UnsupportedGameContent
    {
        public string GameId { get; init; } = "SkyrimSE";
        public string GameName { get; init; }  = "Skyrim Special Edition";
        public string IssuesUrl { get; private init; } = "https://github.com/focustense/easymod/labels/gamefinder";
        public string SupportedDistributionsUrl { get; private init; } = "https://github.com/erri120/GameFinder#gamefinder";

        public UnsupportedGameContent()
        {
        }

        public UnsupportedGameContent(string gameId, string gameName)
        {
            GameId = gameId;
            GameName = gameName;
        }
    }
}
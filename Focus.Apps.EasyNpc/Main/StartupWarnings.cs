using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Main
{
    public class StartupWarning
    {
        public string Title { get; init; } = string.Empty;
        public object Description { get; init; } = string.Empty;
    }

    public class StartupWarnings
    {
        public static readonly StartupWarning MissingVortexManifest = new()
        {
            Title = "Unsupported vortex launch",
            Description = new MissingVortexManifestContent(),
        };

        public static StartupWarning InvalidCommandLine(IEnumerable<string> errorMessages) => new()
        {
            Title = "Invalid options",
            Description = new InvalidCommandLineContent { ErrorMessages = errorMessages },
        };

        public static StartupWarning MissingGameData(string gameId, string gameName) => new()
        {
            Title = "Game not found",
            Description = new MissingGameDataContent(gameId, gameName),
        };

        public static StartupWarning UnsupportedGame(string gameId, string gameName) => new()
        {
            Title = "Game not supported",
            Description = new UnsupportedGameContent(gameId, gameName),
        };
    }

    public class InvalidCommandLineContent
    {
        public IEnumerable<string> ErrorMessages { get; init; } = Enumerable.Empty<string>();
    }

    public class MissingVortexManifestContent
    {
        public string ExtensionUrl { get; private init; } = "https://www.nexusmods.com/site/mods/265";
    }

    public class MissingGameDataContent
    {
        public string GameId { get; init; } = "SkyrimSE";
        public string GameName { get; init; }  = "Skyrim Special Edition";
        public string IssuesUrl { get; private init; } = "https://github.com/focustense/easymod/labels/gamefinder";
        public string SupportedDistributionsUrl { get; private init; } = "https://github.com/erri120/GameFinder#gamefinder";

        public MissingGameDataContent()
        {
        }

        public MissingGameDataContent(string gameId, string gameName)
        {
            GameId = gameId;
            GameName = gameName;
        }
    }

    public class UnsupportedGameContent
    {
        public string GameId { get; init; } = "Fallout4";
        public string GameName { get; init; } = "Fallout 4";
        public bool IsGameKnown => !string.IsNullOrEmpty(GameName);

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
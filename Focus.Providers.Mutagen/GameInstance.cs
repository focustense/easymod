using Mutagen.Bethesda;
using System;

namespace Focus.Providers.Mutagen
{
    public class GameInstance : GameSelection
    {
        public string DataDirectory { get; private init; }

        public static GameInstance FromGameId(string gameId, string? dataDirectory = null)
        {
            return FromGameId(new StaticGameLocations(), gameId, dataDirectory);
        }

        public static GameInstance FromGameId(IGameLocations gameLocations, string gameId, string? dataDirectory = null)
        {
            var isValidGameName = Enum.TryParse<GameRelease>(gameId, true, out var gameRelease);
            if (!isValidGameName)
                throw new UnsupportedGameException(gameId);
            if (string.IsNullOrEmpty(dataDirectory))
            {
                if (gameLocations.TryGetDataFolder(gameRelease, out var detectedDirectory))
                    dataDirectory = detectedDirectory;
                else
                    throw new MissingGameDataException(Enum.GetName(gameRelease)!, GetGameName(gameRelease));
            }
            return new GameInstance(gameRelease, dataDirectory);
        }

        public GameInstance(GameRelease gameRelease, string dataDirectory)
            : base(gameRelease)
        {
            DataDirectory = dataDirectory;
        }
    }
}

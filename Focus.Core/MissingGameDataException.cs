using System;
using System.Runtime.Serialization;

namespace Focus
{
    public class MissingGameDataException : Exception
    {
        public string GameId { get; private init; } = string.Empty;
        public string GameName { get; private init; } = string.Empty;

        public MissingGameDataException(string gameId)
            : base(GetMessage(gameId))
        {
            GameId = gameId;
        }

        public MissingGameDataException(string gameId, string gameName)
            : base(GetMessage(gameId))
        {
            GameId = gameId;
            GameName = gameName;
        }

        public MissingGameDataException(string gameId, Exception innerException)
            : base(GetMessage(gameId), innerException)
        {
            GameId = gameId;
        }

        public MissingGameDataException(string gameId, string gameName, Exception innerException)
            : base(GetMessage(gameId), innerException)
        {
            GameId = gameId;
            GameName = gameName;
        }

        protected MissingGameDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string GetMessage(string gameId)
        {
            return $"Couldn't find {gameId} game data folder";
        }
    }
}
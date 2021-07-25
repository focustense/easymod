using System;
using System.Runtime.Serialization;

namespace Focus.Apps.EasyNpc
{
    public class MissingGameException : Exception
    {
        public string GameId { get; private init; }
        public string GameName { get; private init; }

        public MissingGameException(string gameId)
            : base(GetMessage(gameId))
        {
            GameId = gameId;
        }

        public MissingGameException(string gameId, string gameName)
            : base(GetMessage(gameId))
        {
            GameId = gameId;
            GameName = gameName;
        }

        public MissingGameException(string gameId, Exception innerException)
            : base(GetMessage(gameId), innerException)
        {
            GameId = gameId;
        }

        public MissingGameException(string gameId, string gameName, Exception innerException)
            : base(GetMessage(gameId), innerException)
        {
            GameId = gameId;
            GameName = gameName;
        }

        protected MissingGameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        private static string GetMessage(string gameId)
        {
            return $"Couldn't find {gameId} game data folder";
        }
    }
}
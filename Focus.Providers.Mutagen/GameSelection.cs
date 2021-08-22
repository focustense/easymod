using Mutagen.Bethesda;

namespace Focus.Providers.Mutagen
{
    public class GameSelection
    {
        public string GameName { get; private init; }
        public GameRelease GameRelease { get; private init; }

        public GameSelection(GameRelease gameRelease)
        {
            GameRelease = gameRelease;
            GameName = GetGameName(gameRelease);
        }

        protected static string GetGameName(GameRelease gameRelease) => gameRelease switch
        {
            GameRelease.EnderalLE => "Enderal Legendary Edition",
            GameRelease.EnderalSE => "Enderal Special Edition",
            GameRelease.Fallout4 => "Fallout 4",
            GameRelease.Oblivion => "Oblivion",
            GameRelease.SkyrimLE => "Skyrim Legendary Edition",
            GameRelease.SkyrimSE => "Skyrim Special Edition",
            GameRelease.SkyrimVR => "Skyrim VR",
            _ => "Unknown game"
        };
    }
}

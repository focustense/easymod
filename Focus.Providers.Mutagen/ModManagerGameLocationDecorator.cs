using Focus.ModManagers;
using Mutagen.Bethesda;
using Noggog;

namespace Focus.Providers.Mutagen
{
    public class ModManagerGameLocationDecorator : IGameLocations
    {
        private readonly IGameLocations locations;
        private readonly IModManagerConfiguration modManagerConfig;

        public ModManagerGameLocationDecorator(IGameLocations locations, IModManagerConfiguration modManagerConfig)
        {
            this.locations = locations;
            this.modManagerConfig = modManagerConfig;
        }

        public bool TryGetDataFolder(GameRelease gameRelease, out DirectoryPath path)
        {
            if (!string.IsNullOrEmpty(modManagerConfig.GameDataPath))
            {
                path = modManagerConfig.GameDataPath;
                return true;
            }
            return locations.TryGetDataFolder(gameRelease, out path);
        }
    }
}

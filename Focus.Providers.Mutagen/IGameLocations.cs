using Mutagen.Bethesda;
using Mutagen.Bethesda.Installs;
using Noggog;

namespace Focus.Providers.Mutagen
{
    public interface IGameLocations
    {
        bool TryGetDataFolder(GameRelease gameRelease, out DirectoryPath path);
    }

    public class StaticGameLocations : IGameLocations
    {
        public bool TryGetDataFolder(GameRelease gameRelease, out DirectoryPath path)
        {
            return GameLocations.TryGetDataFolder(gameRelease, out path);
        }
    }
}

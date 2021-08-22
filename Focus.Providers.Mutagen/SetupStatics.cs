using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using System.Collections.Generic;

namespace Focus.Providers.Mutagen
{
    public interface ISetupStatics
    {
        public IReadOnlyCollection<ModKey> GetBaseMasters(GameRelease gameRelease);
        public IEnumerable<IModListingGetter> GetLoadOrderListings(
            GameRelease gameRelease, DirectoryPath dataDirectory, bool throwOnMissingMasters = true);
    }

    public class SetupStatics : ISetupStatics
    {
        public IReadOnlyCollection<ModKey> GetBaseMasters(GameRelease gameRelease)
        {
            return Implicits.Get(gameRelease).BaseMasters;
        }

        public IEnumerable<IModListingGetter> GetLoadOrderListings(
            GameRelease gameRelease, DirectoryPath dataDirectory, bool throwOnMissingMasters = true)
        {
            return LoadOrder.GetListings(gameRelease, dataDirectory, throwOnMissingMasters);
        }
    }
}

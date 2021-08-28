using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace Focus.Providers.Mutagen
{
    public interface IReadOnlyGameEnvironment<TModGetter>
        where TModGetter : class, IModGetter
    {
        DirectoryPath DataFolderPath { get; }
        FilePath LoadOrderFilePath { get; }
        FilePath? CreationClubListingsFilePath { get; }
        public ILoadOrder<IModListing<TModGetter>> LoadOrder { get; }
        public ILinkCache LinkCache { get; }
    }

    public interface IMutableGameEnvironment<TMod, TModGetter> : IReadOnlyGameEnvironment<TModGetter>
        where TModGetter : class, IModGetter
        where TMod : class, IMod, TModGetter
    {
        new ILinkCache<TMod, TModGetter> LinkCache { get; }
    }
}

using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace Focus.Providers.Mutagen
{
    public static class GameEnvironmentWrapper
    {
        public static GameEnvironmentWrapper<TModSetter, TModGetter> Wrap<TModSetter, TModGetter>(
            IGameEnvironment<TModSetter, TModGetter> env)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            return new GameEnvironmentWrapper<TModSetter, TModGetter>(env);
        }
    }

    public class GameEnvironmentWrapper<TModSetter, TModGetter> :
        IMutableGameEnvironment<TModSetter, TModGetter>
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
    {
        public IReadOnlyGameEnvironment<TModGetter> AsReadOnly => this;
        public FilePath? CreationClubListingsFilePath => env.CreationClubListingsFilePath;
        public DirectoryPath DataFolderPath => env.DataFolderPath;
        public FilePath LoadOrderFilePath => env.LoadOrderFilePath;
        public ILoadOrder<IModListing<TModGetter>> LoadOrder => env.LoadOrder;
        public ILinkCache<TModSetter, TModGetter> LinkCache => env.LinkCache;

        ILinkCache IReadOnlyGameEnvironment<TModGetter>.LinkCache => env.LinkCache;

        private readonly IGameEnvironment<TModSetter, TModGetter> env;

        public GameEnvironmentWrapper(IGameEnvironment<TModSetter, TModGetter> env)
        {
            this.env = env;
        }
    }
}

using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace Focus.Providers.Mutagen
{
    public static class GameEnvironmentWrapper
    {
        public static GameEnvironmentWrapper<TModSetter, TModGetter> Wrap<TModSetter, TModGetter>(
            GameEnvironmentState<TModSetter, TModGetter> env)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            return new GameEnvironmentWrapper<TModSetter, TModGetter>(env);
        }
    }

    public class GameEnvironmentWrapper<TModSetter, TModGetter> :
        IReadOnlyGameEnvironment<TModGetter>
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
    {
        public IReadOnlyGameEnvironment<TModGetter> AsReadOnly => this;
        public DirectoryPath DataFolderPath => env.DataFolderPath;
        public FilePath LoadOrderFilePath => env.LoadOrderFilePath;
        public FilePath? CreationKitLoadOrderFilePath => env.CreationKitLoadOrderFilePath;
        public ILoadOrder<IModListing<TModGetter>> LoadOrder => env.LoadOrder;
        public ILinkCache LinkCache => env.LinkCache;

        private readonly GameEnvironmentState<TModSetter, TModGetter> env;

        public GameEnvironmentWrapper(GameEnvironmentState<TModSetter, TModGetter> env)
        {
            this.env = env;
        }
    }
}

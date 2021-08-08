using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen
{
    public static class GameSettings
    {
        public static GameSettings<TModGetter> From<TModGetter>(
            IReadOnlyGameEnvironment<TModGetter> env, GameRelease gameRelease)
            where TModGetter : class, IModGetter
        {
            return new GameSettings<TModGetter>(env, ArchiveStaticsWrapper.Instance, gameRelease);
        }
    }

    public class GameSettings<TModGetter> : IGameSettings
        where TModGetter : class, IModGetter
    {
        private readonly IArchiveStatics archive;
        private readonly IReadOnlyGameEnvironment<TModGetter> env;
        private readonly GameRelease gameRelease;
        private readonly IReadOnlyList<FileName> order;

        public GameSettings(IReadOnlyGameEnvironment<TModGetter> env, IArchiveStatics archive, GameRelease gameRelease)
        {
            this.archive = archive;
            this.env = env;
            this.gameRelease = gameRelease;
            order = archive.GetIniListings(gameRelease)
                .Concat(env.LoadOrder.ListedOrder.SelectMany(x => new[] {
                    // Not all of these will exist, but it doesn't matter, as these are only used for sorting and won't
                    // affect the actual set of paths returned.
                    new FileName($"{x.ModKey.Name}.bsa"),
                    new FileName($"{x.ModKey.Name} - Textures.bsa"),
                }))
                .ToList();
        }

        public IEnumerable<string> ArchiveOrder => archive
            .GetApplicableArchivePaths(gameRelease, env.DataFolderPath, order)
            .Select(x => x.Name.String);
        public string DataDirectory => env.GetRealDataDirectory();
        public IEnumerable<string> PluginLoadOrder => env.LoadOrder.ListedOrder.Select(x => x.ModKey.FileName.String);
    }
}

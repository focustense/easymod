using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen
{
    public static class GameSettings
    {
        public static GameSettings<TModGetter> From<TModGetter>(
            IReadOnlyGameEnvironment<TModGetter> env, GameSelection game)
            where TModGetter : class, IModGetter
        {
            return new GameSettings<TModGetter>(env, ArchiveStatics.Instance, game);
        }
    }

    public class GameSettings<TModGetter> : IGameSettings
        where TModGetter : class, IModGetter
    {
        private readonly IArchiveStatics archive;
        private readonly IReadOnlyGameEnvironment<TModGetter> env;
        private readonly GameSelection game;
        private readonly HashSet<FileName> iniListings;
        private readonly IReadOnlyList<FileName> order;

        public GameSettings(IReadOnlyGameEnvironment<TModGetter> env, IArchiveStatics archive, GameSelection game)
        {
            this.archive = archive;
            this.env = env;
            this.game = game;
            var iniListings = archive.GetIniListings(game.GameRelease).ToList();
            this.iniListings = iniListings.ToHashSet();
            order = iniListings
                .Concat(env.LoadOrder.ListedOrder.SelectMany(x => new[] {
                    // Not all of these will exist, but it doesn't matter, as these are only used for sorting and won't
                    // affect the actual set of paths returned.
                    new FileName($"{x.ModKey.Name}.bsa"),
                    new FileName($"{x.ModKey.Name} - Textures.bsa"),
                }))
                .ToList();
        }

        public IEnumerable<string> ArchiveOrder => archive
            .GetApplicableArchivePaths(game.GameRelease, env.DataFolderPath, order)
            .Select(x => x.Name.String);
        public string DataDirectory => env.GetRealDataDirectory();
        public IEnumerable<string> PluginLoadOrder => env.LoadOrder.ListedOrder.Select(x => x.ModKey.FileName.String);

        public bool IsBaseGameArchive(string archiveName)
        {
            return iniListings.Contains(archiveName);
        }
    }
}

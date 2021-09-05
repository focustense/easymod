using Focus.Environment;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Linq;

namespace Focus.Providers.Mutagen
{
    public interface IEnvironmentFactory
    {
        // This signature is temporary until more fine-grained interfaces can be defined and used everywhere.
        // IReadOnlyGameEnvironment does exist, but some code still requires the full GameEnvironmentState.
        GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> CreateEnvironment();
    }

    public class EnvironmentFactory : IEnvironmentFactory
    {
        private readonly GameSelection game;
        private readonly IGameSetup setup;

        public EnvironmentFactory(IGameSetup setup, GameSelection game)
        {
            this.game = game;
            this.setup = setup;
        }

        public GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> CreateEnvironment()
        {
            if (!setup.IsConfirmed)
                throw new InvalidOperationException(
                    "Attempted to create the game environment before settings were confirmed.");
            var loadOrderKeys = setup.AvailablePlugins
                .Where(p => setup.LoadOrderGraph.IsEnabled(p.FileName) && setup.LoadOrderGraph.CanLoad(p.FileName))
                .Select(p => ModKey.FromNameAndExtension(p.FileName));
            var loadOrder = LoadOrder.Import<ISkyrimModGetter>(setup.DataDirectory, loadOrderKeys, game.GameRelease);
            var linkCache = loadOrder.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
            // If we actually managed to get here, then earlier code already managed to find the listings file.
            var listingsFile = PluginListings.GetListingsFile(game.GameRelease);
            var creationClubFile =
                CreationClubListings.GetListingsPath(game.GameRelease.ToCategory(), setup.DataDirectory);
            return new GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>(
                game.GameRelease, setup.DataDirectory, listingsFile, creationClubFile, loadOrder, linkCache, true);
        }
    }
}

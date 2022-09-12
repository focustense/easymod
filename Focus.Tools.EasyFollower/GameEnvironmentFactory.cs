using Focus.ModManagers.ModOrganizer;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Diagnostics;
using System.IO.Abstractions;

namespace Focus.Tools.EasyFollower
{
    static class GameEnvironmentFactory
    {
        public static IGameEnvironment<ISkyrimMod, ISkyrimModGetter> CreateGameEnvironment(string gameId, ILogger logger)
        {
            var gameLocations = InferGameLocations();
            var gameInstance = GameInstance.FromGameId(gameLocations, gameId);
            if (!IsSupportedGame(gameInstance.GameRelease))
                throw new UnsupportedGameException(gameId, gameInstance.GameName);
            return CreateGameEnvironment(gameInstance, logger);
        }

        private static IGameEnvironment<ISkyrimMod, ISkyrimModGetter> CreateGameEnvironment(GameInstance game, ILogger logger)
        {
            var setup = new GameSetup(new FileSystem(), new SetupStatics(), game, logger);
            setup.Detect(new HashSet<string>());
            setup.Confirm();
            return new EnvironmentFactory(setup, game).CreateEnvironment();
        }

        private static IGameLocations InferGameLocations()
        {
            var defaultGameLocations = new StaticGameLocations();
            var parentProcess = Process.GetCurrentProcess().Parent();
            if (parentProcess == null)
                return defaultGameLocations;
            var isModOrganizer = parentProcess.ProcessName.Equals(
                "modorganizer", StringComparison.OrdinalIgnoreCase);
            if (!isModOrganizer)
                return defaultGameLocations;
            var modManagerConfig = IniConfiguration.AutoDetect(parentProcess.MainModule?.FileName ?? "");
            return new ModManagerGameLocationDecorator(defaultGameLocations, modManagerConfig);
        }

        private static bool IsSupportedGame(GameRelease release) => release switch
        {
            GameRelease.SkyrimSE or GameRelease.SkyrimVR or GameRelease.EnderalSE => true,
            _ => false
        };
    }
}

using Focus.Files;
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
    class AppContainer : IDisposable
    {
        public static AppContainer Create(string gameId, ILogger logger)
        {
            var gameLocations = InferGameLocations();
            var gameInstance = GameInstance.FromGameId(gameLocations, gameId);
            if (!IsSupportedGame(gameInstance.GameRelease))
                throw new UnsupportedGameException(gameId, gameInstance.GameName);
            return Create(gameInstance, logger);
        }

        private static AppContainer Create(GameInstance game, ILogger logger)
        {
            var setup = new GameSetup(new FileSystem(), new SetupStatics(), game, logger);
            setup.Detect(new HashSet<string>());
            setup.Confirm();
            var env = new EnvironmentFactory(setup, game).CreateEnvironment();
            var gameSettings = new GameSettings<ISkyrimModGetter>(
                GameEnvironmentWrapper.Wrap(env), new ArchiveStatics(), game);
            var archiveProvider =
                new CachingArchiveProvider(new MutagenArchiveProvider(game, logger));
            var fileProvider = new GameFileProvider(gameSettings, archiveProvider);
            return new AppContainer(env, fileProvider);
        }

        public IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment { get; }
        public IFileProvider FileProvider { get; }

        private AppContainer(
            IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
            IFileProvider fileProvider)
        {
            Environment = env;
            FileProvider = fileProvider;
        }

        public void Dispose()
        {
            Environment.Dispose();
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

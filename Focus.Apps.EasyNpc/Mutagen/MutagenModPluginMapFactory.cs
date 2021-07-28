using Focus.Apps.EasyNpc.GameData.Files;
using Focus.ModManagers;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenModPluginMapFactory : IModPluginMapFactory
    {
        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;
        private readonly GameRelease gameRelease;
        private readonly IModResolver modResolver;

        public MutagenModPluginMapFactory(
            GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, GameRelease gameRelease,
            IModResolver modResolver)
        {
            this.environment = environment;
            this.gameRelease = gameRelease;
            this.modResolver = modResolver;
        }

        public ModPluginMap CreateForDirectory(string modRootDirectory)
        {
            var pluginNames = environment.LoadOrder.Select(x => x.Key.FileName);
            var archiveNames = Archive
                .GetApplicableArchivePaths(gameRelease, environment.DataFolderPath)
                .Select(f => Path.GetFileName(f));
            return ModPluginMap.ForDirectory(
                modRootDirectory, modResolver, pluginNames.Select(f => f.String), archiveNames);
        }
    }
}

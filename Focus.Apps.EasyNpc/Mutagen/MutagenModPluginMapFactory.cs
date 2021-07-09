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
        private readonly IModResolver modResolver;

        public MutagenModPluginMapFactory(
            GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment, IModResolver modResolver)
        {
            this.environment = environment;
            this.modResolver = modResolver;
        }

        public ModPluginMap CreateForDirectory(string modRootDirectory)
        {
            var pluginNames = environment.LoadOrder.Select(x => x.Key.FileName);
            var archiveNames = Archive
                .GetApplicableArchivePaths(GameRelease.SkyrimSE, environment.DataFolderPath)
                .Select(f => Path.GetFileName(f));
            return ModPluginMap.ForDirectory(
                modRootDirectory, modResolver, pluginNames.Select(f => f.String), archiveNames);
        }
    }
}

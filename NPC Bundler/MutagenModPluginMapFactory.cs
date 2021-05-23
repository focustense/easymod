using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace NPC_Bundler
{
    public class MutagenModPluginMapFactory : IModPluginMapFactory
    {
        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;

        public MutagenModPluginMapFactory(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment)
        {
            this.environment = environment;
        }

        public ModPluginMap CreateForDirectory(string modRootDirectory)
        {
            var pluginNames = environment.LoadOrder.Select(x => x.Key.FileName);
            var archiveNames = environment.LoadOrder.SelectMany(x =>
                Archive.GetApplicableArchivePaths(GameRelease.SkyrimSE, environment.GameFolderPath, x.Key));
            return ModPluginMap.ForDirectory(modRootDirectory, pluginNames, archiveNames);
        }
    }
}

using Focus.Apps.EasyNpc.GameData.Files;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
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
            var archiveNames = Archive
                .GetApplicableArchivePaths(GameRelease.SkyrimSE, environment.GameFolderPath, ModKey.Null)
                .Select(f => Path.GetFileName(f));
            return ModPluginMap.ForDirectory(modRootDirectory, pluginNames, archiveNames);
        }
    }
}

using Focus.Apps.EasyNpc.Configuration;

namespace Focus.Apps.EasyNpc
{
    public interface IModPluginMapFactory
    {
        ModPluginMap CreateForDirectory(string modRootDirectory);
    }

    public static class ModPluginMapFactoryExtensions
    {
        public static ModPluginMap DefaultMap(this IModPluginMapFactory modPluginMapFactory)
        {
            return modPluginMapFactory.CreateForDirectory(BundlerSettings.Default.ModRootDirectory);
        }
    }
}
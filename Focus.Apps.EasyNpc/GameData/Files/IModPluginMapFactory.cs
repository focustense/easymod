using Focus.Apps.EasyNpc.Configuration;

namespace Focus.Apps.EasyNpc.GameData.Files
{
    public interface IModPluginMapFactory
    {
        ModPluginMap CreateForDirectory(string modRootDirectory);
    }

    public static class ModPluginMapFactoryExtensions
    {
        public static ModPluginMap DefaultMap(this IModPluginMapFactory modPluginMapFactory)
        {
            return modPluginMapFactory.CreateForDirectory(Settings.Default.ModRootDirectory);
        }
    }
}
using Focus.ModManagers;

namespace Focus.Apps.EasyNpc.Configuration
{
    public interface IModSettings
    {
        string RootDirectory { get; }
    }

    public class ModSettings : IModSettings
    {
        public string RootDirectory => GetModRootDirectory();

        private readonly IAppSettings appSettings;
        private readonly IModManagerConfiguration modManagerConfig;

        public ModSettings(IAppSettings appSettings, IModManagerConfiguration modManagerConfig)
        {
            this.appSettings = appSettings;
            this.modManagerConfig = modManagerConfig;
        }

        private string GetModRootDirectory()
        {
            if (!appSettings.UseModManagerForModDirectory)
                return appSettings.DefaultModRootDirectory;
            return !string.IsNullOrEmpty(modManagerConfig.ModsDirectory) ?
                modManagerConfig.ModsDirectory : appSettings.DefaultModRootDirectory;
        }
    }
}

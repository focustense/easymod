using Focus.ModManagers;
using System;
using System.Reactive.Linq;

namespace Focus.Apps.EasyNpc.Configuration
{
    public interface IModSettings
    {
        string RootDirectory { get; }
    }

    public interface IObservableModSettings : IModSettings
    {
        IObservable<string> RootDirectoryObservable { get; }
    }

    public class ModSettings : IObservableModSettings
    {
        public string RootDirectory => GetModRootDirectory();
        public IObservable<string> RootDirectoryObservable { get; private init; }

        private readonly IObservableAppSettings appSettings;
        private readonly IModManagerConfiguration modManagerConfig;

        public ModSettings(IObservableAppSettings appSettings, IModManagerConfiguration modManagerConfig)
        {
            this.appSettings = appSettings;
            this.modManagerConfig = modManagerConfig;

            RootDirectoryObservable = Observable
                .CombineLatest(
                    appSettings.DefaultModRootDirectoryObservable, appSettings.UseModManagerForModDirectoryObservable,
                    (defaultModRootDirectory, useModManagerDefault) => (defaultModRootDirectory, useModManagerDefault))
                .Select(t => GetModRootDirectory(t.defaultModRootDirectory, t.useModManagerDefault));
        }

        private string GetModRootDirectory()
        {
            return GetModRootDirectory(appSettings.DefaultModRootDirectory, appSettings.UseModManagerForModDirectory);
        }

        private string GetModRootDirectory(string defaultModRootDirectory, bool useModManagerDefault)
        {
            if (!useModManagerDefault)
                return defaultModRootDirectory;
            return !string.IsNullOrEmpty(modManagerConfig.ModsDirectory) ?
                modManagerConfig.ModsDirectory : defaultModRootDirectory;
        }
    }
}

using Focus.Abstractions.Windows;
using Microsoft.Win32;
using System;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    public class IniLocator
    {
        public static readonly IniLocator Default = new(EnvironmentStatics.Default, RegistryKeyStatics.Default);

        private readonly IEnvironmentStatics environment;
        private readonly IRegistryKeyStatics registry;

        public IniLocator(IEnvironmentStatics environment, IRegistryKeyStatics registry)
        {
            this.environment = environment;
            this.registry = registry;
        }

        public string DetectIniPath(string exePath)
        {
            var instanceName = GetCurrentInstanceName();
            return !string.IsNullOrEmpty(instanceName) ? GetInstanceIniPath(instanceName) : GetPortableIniPath(exePath);
        }

        private string? GetCurrentInstanceName()
        {
            using var hkcu = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            return
                GetKeyValue(hkcu, @"SOFTWARE\Mod Organizer Team\Mod Organizer", "CurrentInstance") ??
                GetKeyValue(hkcu, @"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance");
        }

        private static string? GetKeyValue(IRegistryKey key, string subkeyPath, string name)
        {
            using var subKey = key.OpenSubKey(subkeyPath);
            return subKey != null ? subKey.GetValue(name, "") as string : null;
        }

        private string GetInstanceIniPath(string instanceName)
        {
            var localAppDataPath = environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppDataPath, "ModOrganizer", instanceName, "ModOrganizer.ini");
        }

        private static string GetPortableIniPath(string exePath)
        {
            var directoryPath = Path.GetDirectoryName(exePath);
            return !string.IsNullOrEmpty(directoryPath) ?
                Path.Combine(directoryPath, "ModOrganizer.ini") : string.Empty;
        }
    }
}

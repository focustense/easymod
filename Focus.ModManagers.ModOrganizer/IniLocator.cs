using Focus.Abstractions.Windows;
using Microsoft.Win32;
using System;
using System.IO.Abstractions;

namespace Focus.ModManagers.ModOrganizer
{
    public class IniLocator
    {
        public static readonly IniLocator Default =
            new(EnvironmentStatics.Default, RegistryKeyStatics.Default, new FileSystem());

        private readonly IEnvironmentStatics environment;
        private readonly IFileSystem fs;
        private readonly IRegistryKeyStatics registry;

        public IniLocator(IEnvironmentStatics environment, IRegistryKeyStatics registry, IFileSystem fs)
        {
            this.environment = environment;
            this.fs = fs;
            this.registry = registry;
        }

        public string DetectIniPath(string exePath)
        {
            var portableOverridePath = fs.Path.Combine(fs.Path.GetDirectoryName(exePath), "portable.txt");
            var instanceName = !fs.File.Exists(portableOverridePath) ? GetCurrentInstanceName() : null;
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
            return fs.Path.Combine(localAppDataPath, "ModOrganizer", instanceName, "ModOrganizer.ini");
        }

        private string GetPortableIniPath(string exePath)
        {
            var directoryPath = fs.Path.GetDirectoryName(exePath);
            return !string.IsNullOrEmpty(directoryPath) ?
                fs.Path.Combine(directoryPath, "ModOrganizer.ini") : string.Empty;
        }
    }
}

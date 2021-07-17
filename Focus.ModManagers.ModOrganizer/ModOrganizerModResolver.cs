using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerModResolver : IModResolver
    {
        private readonly ModOrganizerConfiguration config;

        public ModOrganizerModResolver(string exePath)
        {
            var instanceName = GetCurrentInstanceName();
            var entryIniPath = !string.IsNullOrEmpty(instanceName) ?
                GetInstanceIniPath(instanceName) : GetPortableIniPath(exePath);
            config = File.Exists(entryIniPath) ? new ModOrganizerConfiguration(entryIniPath) : null;
        }

        public string GetDefaultModRootDirectory()
        {
            return config?.ModsDirectory;
        }

        public IEnumerable<string> GetModDirectories(string modName)
        {
            return new[] { modName };
        }

        public string GetModName(string directoryPath)
        {
            return new DirectoryInfo(directoryPath).Name;
        }

        private static string GetCurrentInstanceName()
        {
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            return
                GetKeyValue(hkcu, @"SOFTWARE\Mod Organizer Team\Mod Organizer", "CurrentInstance") ??
                GetKeyValue(hkcu, @"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance");
        }

        private static string GetKeyValue(RegistryKey key, string subkeyPath, string name)
        {
            using var subKey = key.OpenSubKey(subkeyPath);
            return subKey != null ? subKey.GetValue(name, "") as string : null;
        }

        private static string GetInstanceIniPath(string instanceName)
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppDataPath, "ModOrganizer", instanceName, "ModOrganizer.ini");
        }

        private static string GetPortableIniPath(string exePath)
        {
            var directoryPath = Path.GetDirectoryName(exePath);
            return Path.Combine(directoryPath, "ModOrganizer.ini");
        }
    }
}
using IniParser;
using IniParser.Model;
using System;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    class ModOrganizerConfiguration
    {
        private static readonly FileIniDataParser parser = new();

        public string BaseDirectory { get; private init; }
        public string DownloadDirectory { get; private init; }
        public string ModsDirectory { get; private init; }
        public string OverwriteDirectory { get; private init; }
        public string ProfilesDirectory { get; private init; }

        public ModOrganizerConfiguration(string entryIniPath)
        {
            var entryIni = parser.ReadFile(entryIniPath);
            var settings = entryIni["Settings"];
            BaseDirectory = settings["base_directory"] ?? Path.GetDirectoryName(entryIniPath);
            DownloadDirectory = ResolveDirectory(settings, "download_directory", "%BASE_DIR%/downloads", BaseDirectory);
            ModsDirectory = ResolveDirectory(settings, "mod_directory", "%BASE_DIR%/mods", BaseDirectory);
            OverwriteDirectory =
                ResolveDirectory(settings, "overwrite_directory", "%BASE_DIR%/overwrite", BaseDirectory);
            ProfilesDirectory = ResolveDirectory(settings, "profiles_directory", "%BASE_DIR%/profiles", BaseDirectory);
        }

        private static string ResolveDirectory(
            KeyDataCollection settings, string settingName, string defaultValue, string baseDirectory)
        {
            var value = settings[settingName];
            if (string.IsNullOrEmpty(value))
                value = defaultValue ?? string.Empty;
            return value.Replace("%BASE_DIR%", baseDirectory, StringComparison.OrdinalIgnoreCase).Replace('/', '\\');
        }
    }
}
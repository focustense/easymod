using IniParser;
using IniParser.Model;
using System;
using System.IO;

namespace Focus.ModManagers.ModOrganizer
{
    public class IniConfiguration : IModOrganizerConfiguration, IModManagerConfiguration
    {
        private static readonly FileIniDataParser parser = new();

        public string BaseDirectory { get; private init; }
        public string DownloadDirectory { get; private init; }
        public string GameDataPath { get; private init; }
        public string ModsDirectory { get; private init; }
        public string OverwriteDirectory { get; private init; }
        public string ProfilesDirectory { get; private init; }
        public string SelectedProfileName { get; private init; }

        public static IniConfiguration AutoDetect(string exePath)
        {
            var entryIniPath = IniLocator.Default.DetectIniPath(exePath);
            return LoadFromFile(entryIniPath);
        }

        public static IniConfiguration LoadFromFile(string entryIniPath)
        {
            var defaultBaseDirectoryPath = Path.GetDirectoryName(entryIniPath) ?? string.Empty;
            using var fs = File.OpenRead(entryIniPath);
            return LoadFromStream(fs, defaultBaseDirectoryPath);
        }

        public static IniConfiguration LoadFromStream(Stream stream, string defaultBaseDirectoryPath)
        {
            using var reader = new StreamReader(stream);
            var entryIni = parser.ReadData(reader) ?? new IniData();
            return new IniConfiguration(entryIni, defaultBaseDirectoryPath);
        }

        public IniConfiguration(IniData entryIni, string defaultBaseDirectoryPath)
        {
            var settings = entryIni["Settings"] ?? new KeyDataCollection();
            BaseDirectory = settings["base_directory"] ?? defaultBaseDirectoryPath;
            DownloadDirectory = ResolveDirectory(settings, "download_directory", "%BASE_DIR%/downloads", BaseDirectory);
            ModsDirectory = ResolveDirectory(settings, "mod_directory", "%BASE_DIR%/mods", BaseDirectory);
            OverwriteDirectory =
                ResolveDirectory(settings, "overwrite_directory", "%BASE_DIR%/overwrite", BaseDirectory);
            ProfilesDirectory = ResolveDirectory(settings, "profiles_directory", "%BASE_DIR%/profiles", BaseDirectory);

            var general = entryIni["General"] ?? new KeyDataCollection();
            GameDataPath = AddDataSuffix(UnwrapString(general["gamePath"] ?? string.Empty).Replace(@"\\", @"\"));
            SelectedProfileName = UnwrapString(general["selected_profile"] ?? string.Empty);
        }

        private static string AddDataSuffix(string gamePath)
        {
            if (string.IsNullOrEmpty(gamePath))
                return gamePath;
            var leafDirectory = new DirectoryInfo(gamePath).Name;
            return leafDirectory.Equals("data", StringComparison.OrdinalIgnoreCase) ?
                gamePath : Path.Combine(gamePath, "data");
        }

        private static string ResolveDirectory(
            KeyDataCollection settings, string settingName, string defaultValue, string baseDirectory)
        {
            var value = settings[settingName];
            if (string.IsNullOrEmpty(value))
                value = defaultValue ?? string.Empty;
            return value.Replace("%BASE_DIR%", baseDirectory, StringComparison.OrdinalIgnoreCase).Replace('/', '\\');
        }

        private static string UnwrapString(string maybeWrapped)
        {
            return maybeWrapped.StartsWith("@ByteArray(") ? maybeWrapped[11..^1] : maybeWrapped;
        }
    }
}
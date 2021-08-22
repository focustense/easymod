using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerComponentResolver : IComponentResolver
    {
        private static readonly Regex BackupRegex = new("backup[0-9]*$", RegexOptions.Compiled);
        private static readonly string NullModId = "0"; // Used by MO2 to mean "no ID", e.g. non-Nexus.

        private readonly IModOrganizerConfiguration config;
        private readonly IFileSystem fs;
        private readonly string rootPath;

        private readonly Lazy<Task<HashSet<string>?>> enabledModNamesInit;

        public ModOrganizerComponentResolver(IModOrganizerConfiguration config, string rootPath)
            : this(new FileSystem(), config, rootPath)
        {
        }

        public ModOrganizerComponentResolver(IFileSystem fs, IModOrganizerConfiguration config, string rootPath)
        {
            this.config = config;
            this.fs = fs;
            this.rootPath = rootPath;

            enabledModNamesInit = new(() => ReadEnabledModNamesFromProfile());
        }

        public async Task<ModComponentInfo> ResolveComponentInfo(string componentName)
        {
            var enabledModNames = await enabledModNamesInit.Value;
            var isEnabled =
                !BackupRegex.IsMatch(componentName) &&
                (enabledModNames is null || enabledModNames.Contains(componentName));

            var iniParser = new FileIniDataParser();

            var modPath = fs.Path.Combine(rootPath, componentName);
            var metaPath = fs.Path.Combine(rootPath, componentName, "meta.ini");
            if (!fs.File.Exists(metaPath))
                return new ModComponentInfo(
                    new ModLocatorKey(string.Empty, componentName), componentName, componentName, modPath, isEnabled);

            var fileId = string.Empty;
            var modId = string.Empty;
            var installationFile = string.Empty;
            try
            {
                var metaIni = await ReadIniFile(iniParser, metaPath);
                modId = metaIni?["General"]?["modid"] ?? string.Empty;
                installationFile = metaIni?["General"]?["installationFile"] ?? string.Empty;
                fileId = metaIni?["installedFiles"]?[@"1\fileid"] ?? string.Empty;
            }
            catch (Exception) { }

            // Nothing in the mod meta gives us the name - but we can get it from the download meta, if available.
            var modName = string.Empty;
            if (!string.IsNullOrEmpty(installationFile) && !string.IsNullOrEmpty(config.DownloadDirectory))
            {
                var downloadMetaPath = fs.Path.Combine(config.DownloadDirectory, $"{installationFile}.meta");
                try
                {
                    var downloadIni = await ReadIniFile(iniParser, downloadMetaPath);
                    if (string.IsNullOrEmpty(fileId))
                        fileId = downloadIni?["General"]["fileID"] ?? string.Empty;
                    modName = downloadIni?["General"]["modName"] ?? string.Empty;
                }
                catch (Exception) { }
            }

            // For Mod Organizer specifically, if we can't determine the Nexus name, then the next-best choice is the
            // component name (i.e. folder name) which is usually the same, or at least _was_ the same at some point.
            // Returning a key with no mod name is generally bad and causes various problems down the road.
            if (string.IsNullOrEmpty(modName))
                modName = componentName;

            var key = new ModLocatorKey(modId != NullModId ? modId : string.Empty, modName);
            var componentId = !string.IsNullOrEmpty(fileId) ? fileId : componentName;
            return new ModComponentInfo(key, componentId, componentName, modPath, isEnabled);
        }

        private async Task<HashSet<string>?> ReadEnabledModNamesFromProfile()
        {
            if (string.IsNullOrEmpty(config.SelectedProfileName))
                return null;
            var modListPath = fs.Path.Combine(config.ProfilesDirectory, config.SelectedProfileName, "modlist.txt");
            if (!fs.File.Exists(modListPath))
                return null;
            var result = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            using var stream = fs.File.OpenRead(modListPath);
            using var reader = new StreamReader(stream);
            string? nextLine;
            while ((nextLine = await reader.ReadLineAsync()) is not null)
            {
                if (nextLine.StartsWith('+'))
                    result.Add(nextLine[1..]);
            }
            return result;
        }

        private async Task<IniData> ReadIniFile(StreamIniDataParser parser, string fileName)
        {
            if (!fs.File.Exists(fileName))
                return new IniData();
            // The IniParser doesn't directly support async I/O, but since the resolver is going to be processing a
            // whole lot of files, it's very important that we don't depend on blocking I/O.
            // Since the INI files are quite small, one hacky solution is to copy their contents (asynchronously) into
            // a memory stream, and then let the INI parser handle that.
            var bytes = await fs.File.ReadAllBytesAsync(fileName);
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamReader(stream);
            return parser.ReadData(reader);
        }
    }
}

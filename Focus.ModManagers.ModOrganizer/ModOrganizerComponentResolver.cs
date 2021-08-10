using IniParser;
using IniParser.Model;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerComponentResolver : IComponentResolver
    {
        private static readonly Regex BackupRegex = new("backup[0-9]*$", RegexOptions.Compiled);
        private static readonly FileIniDataParser IniParser = new();
        private static readonly string NullModId = "0"; // Used by MO2 to mean "no ID", e.g. non-Nexus.

        private readonly IModOrganizerConfiguration config;
        private readonly IFileSystem fs;
        private readonly string rootPath;

        public ModOrganizerComponentResolver(IModOrganizerConfiguration config, string rootPath)
            : this(new FileSystem(), config, rootPath)
        {
        }

        public ModOrganizerComponentResolver(IFileSystem fs, IModOrganizerConfiguration config, string rootPath)
        {
            this.config = config;
            this.fs = fs;
            this.rootPath = rootPath;
        }

        public async Task<ModComponentInfo> ResolveComponentInfo(string componentName)
        {
            // TODO: To actually know if a mod is enabled, we would need to first determine the current profile and then
            // look at that profile's INI. For the moment, this is just a quick fix to prevent backups from appearing.
            var isEnabled = !BackupRegex.IsMatch(componentName);

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
                var metaIni = await ReadIniFile(metaPath);
                modId = metaIni?["General"]?["modid"] ?? string.Empty;
                installationFile = metaIni?["General"]?["installationFile"] ?? string.Empty;
                fileId = metaIni?["installedFiles"]?[@"1\fileid"] ?? string.Empty;
            }
            catch (Exception) { }

            // Nothing in the mod meta gives us the name - but we can get it from the download meta, if available.
            var modName = string.Empty;
            if (!string.IsNullOrEmpty(installationFile))
            {
                try
                {
                    var downloadMetaPath = fs.Path.Combine(config.DownloadDirectory, $"{installationFile}.meta");
                    var downloadIni = await ReadIniFile(downloadMetaPath);
                    if (string.IsNullOrEmpty(fileId))
                        fileId = downloadIni?["General"]["fileID"] ?? string.Empty;
                    modName = downloadIni?["General"]["modName"] ?? string.Empty;
                }
                catch (Exception) { }
            }

            // We need to have _some_ kind of identifier on the mod itself. If it doesn't have an ID, and we were unable
            // to determine the name (not unlikely when there is no ID), then the best choice is usually the same name
            // as the folder, which is Mod Organizer's informal "mod name".
            if (string.IsNullOrEmpty(modId) && string.IsNullOrEmpty(modName))
                modName = componentName;

            var key = new ModLocatorKey(modId != NullModId ? modId : string.Empty, modName);
            var componentId = !string.IsNullOrEmpty(fileId) ? fileId : componentName;
            return new ModComponentInfo(key, componentId, componentName, modPath, isEnabled);
        }

        private async Task<IniData> ReadIniFile(string fileName)
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
            return IniParser.ReadData(reader);
        }
    }
}

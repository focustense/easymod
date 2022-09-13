using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Focus.Tools.EasyFollower
{
    class FollowerConverter
    {
        private static readonly string ExportDataPathPrefix = @"EasyFollower\Exported";
        private static readonly string FaceTintPathPrefix = @"Textures\CharGen\Exported";
        private static readonly string HeadMeshPathPrefix = @"Meshes\CharGen\Exported";
        private static readonly string PresetPathPrefix = @"SKSE\Plugins\CharGen\Exported";

        private static readonly JsonSerializerOptions ExportDataSerializerOptions = new()
        {
            // JContainers is somehow unreliable about casing; even if we put a camelCase key into
            // the map, it may be written as PascalCase or some other case. Possibly something to do
            // with how the game itself caches strings.
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        private static readonly JsonSerializerOptions PresetSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private readonly string dataDirectory;
        private readonly Patcher patcher;
        private readonly ILogger log;

        public FollowerConverter(Patcher patcher, string dataDirectory, ILogger log)
        {
            this.patcher = patcher;
            this.dataDirectory = dataDirectory;
            this.log = log;
        }

        public bool Convert(string fileName, string outputModName, bool backupFiles)
        {
            log.Information("Starting conversion of exported character {fileName}", fileName);
            var filePaths = ValidatePaths(fileName);
            if (filePaths == null)
                return false;
            var exportData = ReadFollowerExportData(filePaths.ExportDataPath);
            if (exportData == null)
                return false;
            var preset = ReadPreset(filePaths.PresetPath);
            if (preset == null)
                return false;
            if (!patcher.Patch(
                fileName, preset, exportData, outputModName, backupFiles, out var npcFormId))
                return false;
            CopyFile(
                filePaths.HeadMeshPath, ResolveHeadMeshPath(outputModName, npcFormId), backupFiles);
            CopyFile(
                filePaths.FaceTintPath, ResolveFaceTintPath(outputModName, npcFormId), backupFiles);
            return true;
        }

        private void CopyFile(string sourcePath, string destPath, bool backupExisting)
        {
            if (backupExisting && File.Exists(destPath))
                File.Copy(
                    destPath,
                    Path.ChangeExtension(destPath, $"{Path.GetExtension(destPath)}.backup"),
                    /* overwrite */ true);
            File.Copy(sourcePath, destPath, /* overwrite */ true);
        }

        private FollowerExportData? ReadFollowerExportData(string fileName)
        {
            using var fs = File.OpenRead(fileName);
            var exportData = JsonSerializer.Deserialize<FollowerExportData>(fs, ExportDataSerializerOptions);
            if (exportData != null)
            {
                log.Information("Loaded export data from {exportDataPath}", fileName);
                log.Debug(JsonSerializer.Serialize(exportData, ExportDataSerializerOptions));
            }
            else
                log.Error("Couldn't read export data from {exportDataPath} (unknown error)", fileName);
            return exportData;
        }

        private RaceMenuPreset? ReadPreset(string fileName)
        {
            using var fs = File.OpenRead(fileName);
            var preset = JsonSerializer.Deserialize<RaceMenuPreset>(fs, PresetSerializerOptions);
            if (preset != null)
            {
                log.Information("Loaded preset from {presetPath}", fileName);
                log.Debug(JsonSerializer.Serialize(preset, PresetSerializerOptions));
            }
            else
                log.Error("Couldn't read preset data from {presetPath} (unknown error)", fileName);
            return preset;
        }

        private string ResolveFaceTintPath(string pluginFileName, string npcFormId)
        {
            return Path.Combine(
                dataDirectory,
                @"textures\actors\character\facegendata\facetint",
                pluginFileName,
                $"00{npcFormId}.dds");
        }

        private string ResolveHeadMeshPath(string pluginFileName, string npcFormId)
        {
            return Path.Combine(
                dataDirectory,
                @"meshes\actors\character\facegendata\facegeom",
                pluginFileName,
                $"00{npcFormId}.nif");
        }

        private FilePaths? ValidatePaths(string fileName)
        {
            var presetPath = Path.Combine(dataDirectory, PresetPathPrefix, $"{fileName}.jslot");
            log.Debug("Using preset path: {presetPath}", presetPath);
            if (!File.Exists(presetPath))
            {
                log.Error("Could not find a RaceMenu preset file at {presetPath}", presetPath);
                return null;
            }
            var exportDataPath = Path.Combine(dataDirectory, ExportDataPathPrefix, $"{fileName}.json");
            log.Debug("Using EasyFollower export data path: {exportDataPath}", exportDataPath);
            if (!File.Exists(exportDataPath))
            {
                log.Error("Could not find the EasyFollower export data at {exportDataPath}", exportDataPath);
                return null;
            }
            var headMeshPath = Path.Combine(dataDirectory, HeadMeshPathPrefix, $"{fileName}.nif");
            log.Debug("Using head mesh path: {headMeshPath}", headMeshPath);
            if (!File.Exists(headMeshPath))
            {
                log.Error("Could not find the head mesh file at {headMeshPath}", headMeshPath);
                return null;
            }
            var faceTintPath = Path.Combine(dataDirectory, FaceTintPathPrefix, $"{fileName}.dds");
            log.Debug("Using face tint path: {faceTintPath}", faceTintPath);
            if (!File.Exists(faceTintPath))
            {
                log.Error("Could not find the face tint file at {faceTintPath}", faceTintPath);
                return null;
            }
            return new FilePaths(presetPath, exportDataPath, headMeshPath, faceTintPath);
        }
    }

    record FilePaths(string PresetPath, string ExportDataPath, string HeadMeshPath, string FaceTintPath);
}

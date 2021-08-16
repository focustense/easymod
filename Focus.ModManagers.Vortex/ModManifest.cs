using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Focus.ModManagers.Vortex
{
    public class ModManifest : IModManagerConfiguration
    {
        private static readonly JsonSerializer Serializer = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };

        [JsonIgnore] // Used only for IModManagerConfiguration, not in file.
        public string ModsDirectory => StagingDir;

        public Dictionary<string, FileInfo> Files { get; set; } = new();
        public Dictionary<string, ModInfo> Mods { get; set; } = new();
        public string StagingDir { get; set; } = string.Empty;

        public static ModManifest LoadFromFile(string path)
        {
            return LoadFromFile(new FileSystem(), path);
        }

        public static ModManifest LoadFromFile(IFileSystem fs, string path)
        {
            using var stream = fs.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            var manifest = Serializer.Deserialize<ModManifest>(jsonReader);
            if (manifest is null)
                throw new ModManagerException($"Failed to deserialize bootstrap file at {path}");
            return manifest;
        }
    }

    public class FileInfo
    {
        public string? ModId { get; set; }
    }

    public class ModInfo
    {
        public string? Name { get; set; }
    }
}
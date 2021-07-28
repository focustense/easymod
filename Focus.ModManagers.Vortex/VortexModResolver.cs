using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.ModManagers.Vortex
{
    public class VortexModResolver : IModResolver
    {
        private static readonly JsonSerializer Serializer = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };

        private readonly IModResolver defaultResolver;
        private readonly ModManifest? manifest;

        public VortexModResolver(IModResolver defaultResolver, string manifestPath)
        {
            this.defaultResolver = defaultResolver;
            manifest = LoadManifest(manifestPath);
        }

        public string? GetDefaultModRootDirectory()
        {
            return manifest?.StagingDir;
        }

        public IEnumerable<string> GetModDirectories(string modName)
        {
            return manifest?.Files
                .Where(f =>
                    !string.IsNullOrEmpty(f.Value.ModId) &&
                    manifest.Mods.TryGetValue(f.Value.ModId, out var modInfo) &&
                    modInfo.Name == modName)
                .Select(f => f.Key)
                ?? Enumerable.Empty<string>();
        }

        public string GetModName(string directoryPath)
        {
            if (manifest == null)
                return defaultResolver.GetModName(directoryPath);
            var directoryName = new DirectoryInfo(directoryPath).Name;
            return
                manifest.Files.TryGetValue(directoryName, out var fileInfo) &&
                !string.IsNullOrEmpty(fileInfo.ModId) &&
                manifest.Mods.TryGetValue(fileInfo.ModId, out var modInfo) &&
                !string.IsNullOrEmpty(modInfo.Name) ?
                modInfo.Name : defaultResolver.GetModName(directoryPath);
        }

        private static ModManifest? LoadManifest(string path)
        {
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fs);
            using var jsonReader = new JsonTextReader(streamReader);
            return Serializer.Deserialize<ModManifest>(jsonReader);
        }
    }
}
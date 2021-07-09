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
        private readonly ModManifest manifest;

        public VortexModResolver(IModResolver defaultResolver, string manifestPath)
        {
            this.defaultResolver = defaultResolver;
            manifest = LoadManifest(manifestPath);
        }

        public IEnumerable<string> GetModDirectories(string modName)
        {
            return manifest.Files
                .Where(f => manifest.Mods.TryGetValue(f.Value.ModId, out var modInfo) && modInfo.Name == modName)
                .Select(f => f.Key);
        }

        public string GetModName(string directoryPath)
        {
            var directoryName = new DirectoryInfo(directoryPath).Name;
            return
                manifest.Files.TryGetValue(directoryName, out var fileInfo) &&
                manifest.Mods.TryGetValue(fileInfo.ModId, out var modInfo) ?
                modInfo.Name : defaultResolver.GetModName(directoryPath);
        }

        private ModManifest LoadManifest(string path)
        {
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fs);
            using var jsonReader = new JsonTextReader(streamReader);
            return Serializer.Deserialize<ModManifest>(jsonReader);
        }
    }
}
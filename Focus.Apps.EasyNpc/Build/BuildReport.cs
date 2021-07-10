using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildReport
    {
        private static readonly JsonSerializer Serializer = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
            Formatting = Formatting.Indented,
        };

        public string ModName { get; init; }

        public void SaveToFile(string fileName)
        {
            using var fs = File.Create(fileName);
            using var streamWriter = new StreamWriter(fs);
            Serializer.Serialize(streamWriter, this);
        }
    }
}
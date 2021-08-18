using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

        public string ModName { get; init; } = string.Empty;

        public void SaveToFile(string fileName)
        {
            using var fs = File.Create(fileName);
            SaveToStream(fs);
        }

        public void SaveToStream(Stream stream)
        {
            using var streamWriter = new StreamWriter(stream);
            Serializer.Serialize(streamWriter, this);
        }
    }
}
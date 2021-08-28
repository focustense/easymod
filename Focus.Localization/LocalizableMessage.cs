using Newtonsoft.Json;

namespace Focus.Localization
{
    public class LocalizableMessage
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string Source { get; set; }
        [JsonIgnore]
        public uint SourceIndex { get; set; }
        public string Translation { get; set; }
    }
}
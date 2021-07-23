using Focus.Apps.EasyNpc.GameData.Records;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Focus.Apps.EasyNpc.Profile
{
    public enum NpcProfileField { DefaultPlugin, FaceMod, FacePlugin };

    public record ProfileEvent : IRecordKey
    {
        public static ProfileEvent Deserialize(string serialized)
        {
            return JsonConvert.DeserializeObject<ProfileEvent>(serialized);
        }

        [JsonProperty("master")]
        public string BasePluginName { get; init; }

        [JsonProperty("id")]
        public string LocalFormIdHex { get; init; }

        [JsonProperty("time")]
        public DateTime Timestamp { get; init; }

        [JsonProperty("field")]
        [JsonConverter(typeof(StringEnumConverter))]
        public NpcProfileField Field { get; init; }

        [JsonProperty("oldValue")]
        public string OldValue { get; init; }

        [JsonProperty("newValue")]
        public string NewValue { get; init; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Focus.Apps.EasyNpc.Configuration
{
    public class SafeStringEnumConverter : StringEnumConverter
    {
        public object DefaultValue { get; }

        public SafeStringEnumConverter(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException)
            {
                return DefaultValue;
            }
        }
    }
}

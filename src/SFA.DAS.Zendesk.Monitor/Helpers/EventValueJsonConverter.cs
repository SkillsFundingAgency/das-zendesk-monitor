using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    /// <summary>
    /// Zendesk is a bit loose with their use of the "value" field in the
    /// "event" structure.  Sometimes it is a string, sometimes an array,
    /// and sometimes an object.
    ///
    /// JsonConvert complains at this.
    ///
    /// Values of the Audit.Event.Value seen in the wild so far:
    ///
    /// "value": ["data_lock____data_lock_multiple", "query"]
    /// "value": "360000656319"
    /// "value": null
    /// "value": {
    ///     "minutes": 3600,
    ///     "in_business_hours": true
    ///  }
    /// </summary>
    public class EventValueJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object? ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "JsonReader cannot be null.");
            }
            
            return reader.TokenType switch
            {
                JsonToken.String => DeserialiseString(reader),
                JsonToken.StartArray => DeserialiseArray(reader),
                _ => DeserialiseObject(reader),
            };
        }

        private static string DeserialiseString(JsonReader reader)
        {
            var token = JToken.Load(reader);
            return token.Type == JTokenType.Null ? string.Empty : token.ToObject<string>() ?? string.Empty;
        }

        private static string DeserialiseArray(JsonReader reader)
        {
            var values = JArray.Load(reader).ToObject<IList<object>>();
            return values != null ? string.Join(",", values) : string.Empty;
        }

        private static string DeserialiseObject(JsonReader reader)
            => JToken.Load(reader).ToObject<object>()?.ToString() ?? "";

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException(
                @$"Serialisation has not been implemented for `string` named ""Value""");
        }
    }

}
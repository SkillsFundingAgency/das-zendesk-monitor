using Newtonsoft.Json;
using System;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    /// <summary>
    /// Add a root object around the default serialisation of T.
    /// </summary>
    /// <typeparam name="T">The type to serialise.</typeparam>
    public class RootObjectConverter<T> : JsonConverter
        where T : class
    {
        private readonly string rootName;

        public RootObjectConverter(string rootName)
            => this.rootName = rootName;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(T);

        public override bool CanRead => false;

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        => throw new NotImplementedException("Deserialising is not supported");

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            var safe = value as T
                ?? throw new ArgumentException(
                    $"`value` must be of type {nameof(T)}");

            writer.WriteStartObject();
            writer.WritePropertyName(rootName);
            serializer.Serialize(writer, NonCircularSerialisation(safe));
            writer.WriteEndObject();
        }

        /// <summary>
        /// We cannot simply pass the instance of T back into the serializer.  
        /// This would create a cycle because the serializer is configured to
        /// use this class again to serialize T.  Instead we create a dictionary
        /// of all the public properties from the T.
        /// </summary>
        /// <param name="t">The object to anonymise.</param>
        /// <returns>All the public properties from the T.</returns>
        private static object NonCircularSerialisation(T t)
            => typeof(T)
                .GetProperties()
                .ToDictionary(
                    x => x.Name,
                    x => x.GetValue(t));
    }
}
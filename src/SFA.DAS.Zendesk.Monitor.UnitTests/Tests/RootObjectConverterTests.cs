using Newtonsoft.Json;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.Tests
{
    public class RootObjectConverterTests
    {
        private class Person
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        [Fact]
        public void CanConvert_ShouldReturnTrue_ForMatchingType()
        {
            var converter = new RootObjectConverter<Person>("person");
            Assert.True(converter.CanConvert(typeof(Person)));
        }

        [Fact]
        public void CanConvert_ShouldReturnFalse_ForDifferentType()
        {
            var converter = new RootObjectConverter<Person>("person");
            Assert.False(converter.CanConvert(typeof(string)));
        }

        [Fact]
        public void CanRead_ShouldReturnFalse()
        {
            var converter = new RootObjectConverter<Person>("person");
            Assert.False(converter.CanRead);
        }

        [Fact]
        public void ReadJson_ShouldThrowNotImplementedException()
        {
            var converter = new RootObjectConverter<Person>("person");
            Assert.Throws<NotImplementedException>(() =>
                converter.ReadJson(null, typeof(Person), null, null));
        }

        [Fact]
        public void WriteJson_ShouldWrapObjectWithRoot()
        {
            var person = new Person { Name = "Alice", Age = 30 };
            var converter = new RootObjectConverter<Person>("person");
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { converter }
            };

            var result = JsonConvert.SerializeObject(person, settings);

            Assert.Equal("{\"person\":{\"Name\":\"Alice\",\"Age\":30}}", result);
        }

        [Fact]
        public void WriteJson_ShouldThrowIfWriterIsNull()
        {
            var converter = new RootObjectConverter<Person>("person");

            Assert.Throws<ArgumentNullException>(() =>
                converter.WriteJson(null, new Person(), new JsonSerializer()));
        }

        [Fact]
        public void WriteJson_ShouldThrowIfSerializerIsNull()
        {
            var converter = new RootObjectConverter<Person>("person");

            using var writer = new JsonTextWriter(new StringWriter());
            Assert.Throws<ArgumentNullException>(() =>
                converter.WriteJson(writer, new Person(), null));
        }

        [Fact]
        public void WriteJson_ShouldThrowIfValueIsWrongType()
        {
            var converter = new RootObjectConverter<Person>("person");

            using var writer = new JsonTextWriter(new StringWriter());
            var serializer = new JsonSerializer();

            Assert.Throws<ArgumentException>(() =>
                converter.WriteJson(writer, new object(), serializer));
        }
    }
}

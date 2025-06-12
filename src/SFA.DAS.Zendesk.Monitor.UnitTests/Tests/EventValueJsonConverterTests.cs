using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class EventValueJsonConverterTests
    {
        private readonly EventValueJsonConverter _converter = new EventValueJsonConverter();

        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(object), false)]
        public void CanConvert_ReturnsExpected(Type type, bool expected)
        {
            Assert.Equal(expected, _converter.CanConvert(type));
        }

        [Fact]
        public void CanWrite_IsFalse()
        {
            Assert.False(_converter.CanWrite);
        }

        [Fact]
        public void WriteJson_ThrowsNotImplementedException()
        {
            var writer = new JTokenWriter();
            Assert.Throws<NotImplementedException>(() =>
                _converter.WriteJson(writer, "value", new JsonSerializer()));
        }

        [Fact]
        public void ReadJson_StringValue_ReturnsString()
        {
            var json = "\"test-value\"";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read(); 

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal("test-value", result);
        }

        [Fact]
        public void ReadJson_NullString_ReturnsEmptyString()
        {
            var json = "null";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read();

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ReadJson_ArrayValue_ReturnsCommaSeparatedString()
        {
            var json = "[\"foo\",\"bar\",42]";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read();

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal("foo,bar,42", result);
        }

        [Fact]
        public void ReadJson_EmptyArray_ReturnsEmptyString()
        {
            var json = "[]";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read();

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ReadJson_ObjectValue_ReturnsMinifiedJson()
        {
            var json = "{\"minutes\":3600,\"in_business_hours\":true}";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read();

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal("{\"minutes\":3600,\"in_business_hours\":true}", result);
        }

        [Fact]
        public void ReadJson_NullObject_ReturnsEmptyString()
        {
            var json = "null";
            var reader = new JsonTextReader(new System.IO.StringReader(json));
            reader.Read();

            var result = _converter.ReadJson(reader, typeof(string), null, new JsonSerializer());

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ReadJson_ThrowsArgumentNullException_WhenReaderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _converter.ReadJson(null, typeof(string), null, new JsonSerializer()));
        }
    }
}

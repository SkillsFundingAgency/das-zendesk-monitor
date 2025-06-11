using Newtonsoft.Json;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class RootObjectConverterTests
    {
        private class Person
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        [Fact]
        public void WriteJson_ShouldWrapObjectWithRoot()
        {
            var person = new Person { Name = "Alice", Age = 30 };
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new RootObjectConverter<Person>("person") },
                Formatting = Formatting.None
            };

            var json = JsonConvert.SerializeObject(person, settings);

            Assert.Equal("{\"person\":{\"Name\":\"Alice\",\"Age\":30}}", json);
        }
    }
}
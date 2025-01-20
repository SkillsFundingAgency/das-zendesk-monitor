using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class ModelSerialisationTests
    {
        // Test the custom deserialization of event values.
        [Theory]
        [InlineData(@"""123456""", "123456")]
        [InlineData(@"[""123"", ""456""]", "123,456")]
        [InlineData(@"{""minutes"":3600,""in_business_hours"":true}", "{\"minutes\": 3600, \"in_business_hours\": true}")]
        [InlineData(@"123456", "123456")]
        [InlineData(@"null", null)]  // Handling null value explicitly
        public void TestCustomDeserializationOfEventValue2(string value, string expected)
        {
            var json = @"{""audits"": [{""events"": [" +
                        @"{""value"": " + value + "}," +
                        @"{""previous_value"": " + value + "}" +
                        @"]}]}";

            var j = JsonConvert.DeserializeObject<Zendesk.Model.AuditResponse>(
                json, Zendesk.ApiFactoryExtensions.serialiser);

            // Deserialize both the value and expected into JToken to handle both simple and complex JSON structures
            JToken expectedValue = expected == null ? null : JToken.Parse(expected);

            // Ensure that the value matches the expected value (it could be a string or a more complex object)
            j.Audits.SelectMany(x => x.Events).Should()
                .ContainEquivalentOf(new { Value = expectedValue });
        }

        // Additional test cases for handling different event values
        [Theory]
        [InlineData(@"""123456""", "123456")]
        [InlineData(@"[ ""123"", ""456"" ]", "123,456")]
        [InlineData(@"null", null)]
        public void TestValueHandling(string value, string expected)
        {
            var json = @"{""audits"": [{""events"": [" +
                        @"{""value"": " + value + "}" +
                        @"]}]}";

            var j = JsonConvert.DeserializeObject<Zendesk.Model.AuditResponse>(
                json, Zendesk.ApiFactoryExtensions.serialiser);

            // Handle null case
            JToken expectedValue = expected == null ? null : JToken.Parse(expected);

            // Ensure that the value matches the expected value
            j.Audits.SelectMany(x => x.Events).Should()
                .ContainEquivalentOf(new { Value = expectedValue });
        }

        // Test with different event data
        [Theory]
        [InlineData(@"[{""value"":""12345""},{""value"":""67890""}]", "[\"12345\", \"67890\"]")]
        [InlineData(@"[{""value"":""abc""},{""value"":""def""}]", "[\"abc\", \"def\"]")]
        public void TestMultipleEvents(string value, string expected)
        {
            var json = @"{""audits"": [{""events"": " + value + "}]}";

            var j = JsonConvert.DeserializeObject<Zendesk.Model.AuditResponse>(
                json, Zendesk.ApiFactoryExtensions.serialiser);

            // Ensure the events match the expected array
            JArray expectedArray = JArray.Parse(expected);

            j.Audits.SelectMany(x => x.Events).Select(e => e.Value).Should()
                .ContainEquivalentOf(expectedArray);
        }

        // Test deserialization with complex objects (objects inside arrays)
        [Theory]
        [InlineData(@"{""minutes"":3600,""in_business_hours"":true}", "{\"minutes\": 3600, \"in_business_hours\": true}")]
        [InlineData(@"{""status"":""active"",""count"":42}", "{\"status\":\"active\", \"count\": 42}")]
        public void TestComplexObjectHandling(string value, string expected)
        {
            var json = @"{""audits"": [{""events"": [" +
                        @"{""value"": " + value + "}" +
                        @"]}]}";

            var j = JsonConvert.DeserializeObject<Zendesk.Model.AuditResponse>(
                json, Zendesk.ApiFactoryExtensions.serialiser);

            // Deserialize the expected value as a JToken for flexible comparison
            JToken expectedValue = JToken.Parse(expected);

            // Ensure that the complex value matches the expected structure
            j.Audits.SelectMany(x => x.Events).Should()
                .ContainEquivalentOf(new { Value = expectedValue });
        }
    }
}

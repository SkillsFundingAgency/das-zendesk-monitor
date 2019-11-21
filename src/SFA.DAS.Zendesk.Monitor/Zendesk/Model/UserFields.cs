#nullable disable
using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class UserFields
    {
        [JsonProperty("Address_Line_1")]
        public object AddressLine1 { get; set; }

        [JsonProperty("Address_Line_2")]
        public object AddressLine2 { get; set; }

        [JsonProperty("Address_Line_3")]
        public object AddressLine3 { get; set; }

        public object City { get; set; }

        public object ContactType { get; set; }

        public object County { get; set; }

        public object Postcode { get; set; }
    }
}
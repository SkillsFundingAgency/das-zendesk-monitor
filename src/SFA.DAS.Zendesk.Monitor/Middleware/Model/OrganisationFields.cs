using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class OrganizationFields
    {
        [JsonProperty("Address_Line_1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("Address_Line_2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("Address_Line_3")]
        public string AddressLine3 { get; set; }

        public string City { get; set; }
        public string County { get; set; }
        public string Postcode { get; set; }
        public string OrganisationType { get; set; }
        public string MainPhone { get; set; }
        public string OrganisationStatus { get; set; }
    }
}
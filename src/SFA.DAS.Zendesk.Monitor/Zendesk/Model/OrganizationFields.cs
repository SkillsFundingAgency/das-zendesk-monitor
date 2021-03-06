﻿#nullable disable
using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class OrganizationFields
    {
        public object AccountManager { get; set; }

        public object AccountManagerEMail { get; set; }

        public string AccountManagerStatus { get; set; }

        [JsonProperty("Address_Line_1")]
        public object AddressLine1 { get; set; }

        [JsonProperty("Address_Line_2")]
        public object AddressLine2 { get; set; }

        [JsonProperty("Address_Line_3")]
        public object AddressLine3 { get; set; }

        public object City { get; set; }

        public object County { get; set; }

        public string MainPhone { get; set; }

        public string OrganisationStatus { get; set; }

        public string OrganisationType { get; set; }

        public string Postcode { get; set; }
    }
}
#nullable disable

using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public partial class Organization
    {
        public Uri Url { get; set; }

        public long? Id { get; set; }

        public string Name { get; set; }

        public bool? SharedTickets { get; set; }

        public bool? SharedComments { get; set; }

        public object ExternalId { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string[] DomainNames { get; set; }

        public object Details { get; set; }

        public object Notes { get; set; }

        public object GroupId { get; set; }

        public object[] Tags { get; set; }

        public OrganizationFields OrganizationFields { get; set; }
    }
}
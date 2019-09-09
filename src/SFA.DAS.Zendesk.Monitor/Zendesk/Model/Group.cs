using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public partial class Group
    {
        public Uri Url { get; set; }

        public long? Id { get; set; }

        public string Name { get; set; }

        public bool? Deleted { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
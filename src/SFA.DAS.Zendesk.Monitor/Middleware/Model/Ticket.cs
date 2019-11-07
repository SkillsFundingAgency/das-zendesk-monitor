using Newtonsoft.Json;
using System;

namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class Ticket
    {
        public long Id { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Subject { get; set; }
        public Comments[] Comments { get; set; }
        public CustomField[] CustomFields { get; set; }
        public Organisation Organization { get; set; }
        public User Requester { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
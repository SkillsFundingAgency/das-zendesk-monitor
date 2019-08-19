using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class EventWrapper
    {
        public Zendesk.Ticket Ticket { get; set; }

        public Zendesk.Comment[] Comments { get; set; } = new Zendesk.Comment[] { };

        public Zendesk.User Requester { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class EventWrapper
    {
        public Zendesk.Model.Ticket Ticket { get; set; }

        public Zendesk.Model.Comment[] Comments { get; set; } = new Zendesk.Model.Comment[] { };

        public Zendesk.Model.User Requester { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
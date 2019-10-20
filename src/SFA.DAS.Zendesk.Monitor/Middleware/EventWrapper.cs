using Newtonsoft.Json;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class EventWrapper
    {
        public EventWrapper2 Ticket { get; set; }
    }

    public class EventWrapper2 : Zendesk.Model.Ticket
    {
        public Zendesk.Model.Comment[] Comments { get; set; } = new Zendesk.Model.Comment[] { };

        public Zendesk.Model.User Requester { get; set; }

        public Zendesk.Model.Organization Organization { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
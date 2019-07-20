using Newtonsoft.Json;

namespace ZenWatch.Middleware
{
    public class EventWrapper
    {
        public Zendesk.Ticket Ticket { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

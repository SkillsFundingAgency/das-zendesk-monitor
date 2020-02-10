#nullable disable

using Newtonsoft.Json;
using SFA.DAS.Zendesk.Monitor.Middleware.Model;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class EventWrapper
    {
        public Ticket Ticket { get; set; }

        public override string ToString() 
            => JsonConvert.SerializeObject(this);
    }
}
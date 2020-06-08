#nullable disable

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class TicketField
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public bool Active { get; set; }
    }
}
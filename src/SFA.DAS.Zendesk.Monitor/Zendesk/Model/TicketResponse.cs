#nullable disable

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class TicketResponse
    {
        public Ticket Ticket { get; set; }

        public Comment[] Comments { get; set; }

        public User[] Users { get; set; }

        public Organization[] Organizations { get; set; }
        
        public Audit[] Audits { get; set; }
    }

    public class TicketRequest
    {
        public Ticket Ticket { get; set; }
    }

    public class CommentResponse
    {
        public Comment[] Comments { get; set; }
    }

    public class AuditResponse
    {
        public Audit[] Audits { get; set; }
    }

    public class TicketFieldResponse
    {
        public TicketField[] TicketFields { get; set; }
    }

    public class SearchResponse
    {
        public Ticket[] Results { get; set; }

        public static SearchResponse Create(params Ticket[] tickets)
            => new SearchResponse { Results = tickets };
    }
}
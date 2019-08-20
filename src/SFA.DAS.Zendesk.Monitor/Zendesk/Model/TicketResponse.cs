namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class TicketResponse
    {
        public Ticket Ticket { get; set; }

        public Comment[] Comments { get; set; }

        public User[] Users { get; set; }
    }

    public class TicketRequest
    {
        public Ticket Ticket { get; set; }
    }

    public class CommentResponse
    {
        public Comment[] Comments { get; set; }
    }

    public class SearchResponse
    {
        public Ticket[] Results { get; set; }

        public static SearchResponse Create(params Ticket[] tickets)
            => new SearchResponse { Results = tickets };
    }
}
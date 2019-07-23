namespace ZenWatch.Zendesk
{
    public class TicketResponse
    {
        public Ticket Ticket { get; set; }
    }

    public class SearchResponse
    {
        public Ticket[] Results { get; set; }
    }
}
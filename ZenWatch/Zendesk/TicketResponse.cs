﻿namespace ZenWatch.Zendesk
{
    public class TicketResponse
    {
        public Ticket Ticket { get; set; }
    }

    public class SearchResponse
    {
        public Ticket[] Results { get; set; }

        public static SearchResponse Create(params Ticket[] tickets)
            => new SearchResponse { Results = tickets };
    }
}
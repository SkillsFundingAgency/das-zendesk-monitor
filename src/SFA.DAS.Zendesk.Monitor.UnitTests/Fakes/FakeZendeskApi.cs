using RestEase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using System;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class FakeZendeskApi : Zendesk.IApi
    {
        public List<Ticket> Tickets { get; } = new List<Ticket>();

        public Dictionary<long, List<Comment>> Comments { get; } = new Dictionary<long, List<Comment>>();

        public Task<SearchResponse> SearchTickets([Query] string query)
        {
            var response = new SearchResponse { Results = Tickets.ToArray() };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> GetTicket([Path] long id)
        {
            var ticket = Tickets.First(x => x.Id == id);
            var response = new TicketResponse { Ticket = ticket };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> PostTicket([Body] Empty ticket) => Task.FromResult<TicketResponse>(null);

        public Task PutTicket([Path] long id, [Body] Empty ticket) => Task.CompletedTask;

        internal void AddComments(Ticket ticket, Comment[] comments)
        {
            TicketComments(ticket.Id).AddRange(comments);
        }

        private List<Comment> TicketComments(long id) => Comments.GetOrAdd(id, () => new List<Comment>());

        public Comment[] GetTicketComments(long id) => TicketComments(id).ToArray();
    }

    public static class DictionaryExtensions
    {
        public static T GetOrAdd<T>(this Dictionary<long, T> c, long key, Func<T> createNew)
        {
            if (!c.TryGetValue(key, out var ticketComments))
            {
                ticketComments = createNew();
                c[key] = ticketComments;
            }

            return ticketComments;
        }
    }

    /*
     * Watcher marks ticket as sharing before sending to middleware
     * Watcher marks ticket as shared after successfully sending to middleware
     * Watcher leaves ticket as sharing after unsuccessfully sending to middleware
     *
     */
}
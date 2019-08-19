using RestEase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.Zendesk.Monitor.Zendesk;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class FakeZendeskApi : Zendesk.IApi
    {
        public List<Ticket> Tickets { get; } = new List<Ticket>();

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
    }

    /*
     * Watcher marks ticket as sharing before sending to middleware
     * Watcher marks ticket as shared after successfully sending to middleware
     * Watcher leaves ticket as sharing after unsuccessfully sending to middleware
     *
     */
}
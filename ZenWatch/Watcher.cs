using System.Threading.Tasks;
using ZenWatch.Zendesk;

namespace ZenWatch
{
    public class Watcher
    {
        private readonly ISharingTickets zendesk;
        private readonly Middleware.IApi middleware;

        public Watcher(ISharingTickets zendesk, Middleware.IApi middleware)
        {
            this.zendesk = zendesk;
            this.middleware = middleware;
        }

        public async Task Watch()
        {
            var tickets = await GetTicketsForSharing();
            foreach (var ticket in tickets)
            {
                await ShareTicket(ticket);
            }
        }

        public Task<long[]> GetTicketsForSharing() => zendesk.GetTicketsForSharing();

        public async Task ShareTicket(long id)
        {
            var ticket = await zendesk.GetTicketForSharing(id);
            await ShareTicket(ticket);
        }

        private async Task ShareTicket(Ticket ticket)
        {
            await zendesk.MarkSharing(ticket);
            await middleware.PostEvent(new Middleware.EventWrapper { Ticket = ticket });
            await zendesk.MarkShared(ticket);
        }
    }
}
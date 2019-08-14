using System;
using System.Threading.Tasks;
using SFA.DAS.Zendesk.Monitor.Zendesk;

namespace SFA.DAS.Zendesk.Monitor
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

        [Obsolete]
        private async Task Watch()
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

            if (ticket.Tags.Contains("pending_middleware"))
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
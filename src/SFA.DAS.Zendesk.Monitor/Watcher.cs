using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Linq;
using System.Threading.Tasks;

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

        public Task<long[]> GetTicketsForSharing() => zendesk.GetTicketsForSharing();

        public async Task ShareTicket(long id)
        {
            var ticket = await zendesk.GetTicketForSharing(id);

            if (ticket.Ticket.Tags.Contains("pending_middleware") ||
                ticket.Ticket.Tags.Contains("sending_middleware"))
                await ShareTicket(ticket);
        }

        private async Task ShareTicket(TicketResponse ticket)
        {
            await zendesk.MarkSharing(ticket.Ticket);
            await middleware.PostEvent(new Middleware.EventWrapper
            {
                Ticket = ticket.Ticket,
                Comments = ticket.Comments,
                Requester = ticket.Users?.FirstOrDefault(x => x.Id == ticket.Ticket.RequesterId),
                Organization = ticket.Organizations?.FirstOrDefault(x => x.Id == ticket.Ticket.OrganizationId),
            });
            await zendesk.MarkShared(ticket.Ticket);
        }
    }
}
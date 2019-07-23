using System.Collections.Generic;
using System.Linq;
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
            var tickets = await zendesk.GetTicketsForSharing();
            foreach (var ticket in tickets)
            {
                zendesk.MarkSharing(ticket);
                await middleware.PostEvent(new Middleware.EventWrapper { Ticket = ticket });
                zendesk.MarkShared(ticket);
            }
        }

        public IEnumerable<Middleware.EventWrapper> Share(Ticket[] ticketsToBeShared)
        {
            return ticketsToBeShared.Select(x => new Middleware.EventWrapper
            {
                Ticket = x
            });
        }
    }
}
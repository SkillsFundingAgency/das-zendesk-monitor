using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZenWatch.Zendesk;

namespace ZenWatch
{
    public class Watcher
    {
        private readonly Zendesk.IApi api;
        private readonly ISharingTickets zapi;
        private readonly Middleware.IApi mw;

        public Watcher(Zendesk.ISharingTickets api, Middleware.IApi mw)
        {
            this.zapi = api;
            this.mw = mw;
        }

        public async Task Watch()
        {
            var tickets = await zapi.GetTicketsForSharing();
            foreach (var ticket in tickets)
            {
                zapi.MarkSharing(ticket);
                await mw.PostEvent(new Middleware.EventWrapper { Ticket = ticket });
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
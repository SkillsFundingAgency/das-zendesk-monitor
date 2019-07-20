using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZenWatch.Zendesk;

namespace ZenWatch.Zendesk
{
    public interface IZApi
    {
        Task<Ticket[]> GetTicketsForSharing();// => throw new NotImplementedException();
        void MarkSharing(Ticket t);// => throw new NotImplementedException();
        void MarkShared(Ticket t);// => throw new NotImplementedException();
    }

    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket(long id);
    }

    public class Ticket
    {
        public long Id { get; set; }

        public string Url { get; set; }

        public string Subject { get; set; }
    }

    public class TicketResponse
    {
        public Ticket Ticket { get; set; }
    }
}

namespace ZenWatch.Middleware
{
    public interface IApi
    {
        [Post("/event")]
        Task PostEvent([Body] EventWrapper body);
    }

    public class EventWrapper
    {
        public Zendesk.Ticket Ticket { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

namespace ZenWatch
{

    public class Watcher
    {
        private readonly Zendesk.IApi api;
        private readonly IZApi zapi;
        private readonly Middleware.IApi mw;

        public Watcher(Zendesk.IZApi api, Middleware.IApi mw)
        {
            this.zapi = api;
            this.mw = mw;
        }

        //public Watcher(Zendesk.IApi api, Middleware.IApi mw)
        //{
        //    this.api = api;
        //    this.mw = mw;
        //}

        //public async Task Watch()
        //{
        //    var ticket = await api.GetTicket(142);
        //    await mw.PostEvent(new Middleware.EventWrapper { Ticket = ticket.Ticket });
        //}

        public async Task Watch2()
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

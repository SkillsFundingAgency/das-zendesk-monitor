using System.Linq;
using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public class SharingTickets : ISharingTickets
    {
        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api;
        }

        public async Task<Ticket> GetTicketForSharing(long id)
        {
            var response = await api.GetTicket(id);
            return response.Ticket;
        }

        public async Task<long[]> GetTicketsForSharing()
        {
            var response = await api.SearchTickets("tags:pending_middleware");
            return response?.Results?
                .Where(x => x.Tags.Contains("pending_middleware"))
                .Select(x => x.Id).ToArray()
                ?? new long[] { };
        }

        public Task MarkSharing(Ticket t)
        {
            t.Tags.Remove("pending_middleware");
            t.Tags.Add("sending_middleware");
            return api.PutTicket(t.Id, new Empty { Ticket = t });
        }

        public Task MarkShared(Ticket t)
        {
            t.Tags.Remove("sending_middleware");
            return api.PutTicket(t.Id, new Empty { Ticket = t });
        }
    }
}
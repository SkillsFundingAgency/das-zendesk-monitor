using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class SharingTickets : ISharingTickets
    {
        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api;
        }

        public async Task<TicketResponse> GetTicketForSharing(long id)
        {
            var response = await api.GetTicketWithSideloads(id);
            return new TicketResponse
            {
                Ticket = response.Ticket,
                Comments = (await api.GetTicketComments(id)).Comments,
                Requester = response.Requester,
            };
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
            return api.PutTicket(t);
        }

        public Task MarkShared(Ticket t)
        {
            t.Tags.Remove("sending_middleware");
            return api.PutTicket(t);
        }
    }
}
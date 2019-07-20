using Refit;
using Scotch;
using System;
using System.Threading.Tasks;
using ZenWatch.Zendesk;

namespace ZenWatch.Acceptance
{
    internal class CannedZendeskApi : ISharingTickets
    {
        private string file;

        public Task<TicketResponse> GetTicket(long id)
        {
            var httpClient = HttpClients.NewHttpClient($@"c:\temp\scotch{file}.json", ScotchMode.Replaying);
            httpClient.BaseAddress = new Uri("https://esfa.zendesk.com/api/v2");
            return RestService.For<IApi>(httpClient).GetTicket(id);
        }

        public async Task<ZenWatch.Zendesk.Ticket[]> GetTicketsForSharing()
        {
            var httpClient = HttpClients.NewHttpClient($@"c:\temp\scotch{file}.json", ScotchMode.Replaying);
            httpClient.BaseAddress = new Uri("https://esfa.zendesk.com/api/v2");
            var response = await RestService.For<IApi>(httpClient).GetTicket(142);
            return new[] { response.Ticket };
        }

        public void MarkShared(ZenWatch.Zendesk.Ticket t)
        {
            throw new NotImplementedException();
        }

        public void MarkSharing(ZenWatch.Zendesk.Ticket t)
        {
        }

        internal void UseTicket(Ticket ticket)
        {
            file = ticket.File;
        }
    }
}
using Newtonsoft.Json;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class FakeZendeskApiFromCapturedResources : IApi
    {
        public FakeZendeskApiFromCapturedResources(long ticketId)
        {
            TicketId = ticketId;
        }

        public long TicketId { get; }

        public Task<TicketResponse> GetTicket([Path] long id)
        {
            var json = Resources.LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.json");
            return Task.FromResult(JsonConvert.DeserializeObject<TicketResponse>(json));
        }

        public Task<AuditResponse> GetTicketAudits([Path] long id)
        {
            var json = Resources.LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.audits.json");
            return Task.FromResult(JsonConvert.DeserializeObject<AuditResponse>(json));
        }

        public Task<CommentResponse> GetTicketComments([Path] long id)
        {
            var json = Resources.LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.comments.json");
            return Task.FromResult(JsonConvert.DeserializeObject<CommentResponse>(json));
        }

        public Task<TicketResponse> GetTicketWithSideloads([Path] long id, SideLoads include)
            => GetTicket(id);

        public Task<TicketResponse> PostTicket([Body] TicketRequest ticket)
            => GetTicket(ticket.Ticket.Id);

        public Task PutTicket([Path] long id, [Body] TicketRequest ticket)
            => Task.CompletedTask;

        public Task<SearchResponse> SearchTickets([Query] string query) 
            => throw new NotImplementedException();
    }
}
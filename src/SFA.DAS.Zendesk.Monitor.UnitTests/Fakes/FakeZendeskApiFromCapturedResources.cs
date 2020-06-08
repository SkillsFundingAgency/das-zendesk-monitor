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
            => Resources
                .LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.json")
                .DeserialiseZendesk<TicketResponse>()
                .ToResult();

        public Task<AuditResponse> GetTicketAudits([Path] long id)
            => Resources
                .LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.audits.json")
                .DeserialiseZendesk<AuditResponse>()
                .ToResult();

        public Task<CommentResponse> GetTicketComments([Path] long id)
            => Resources
                .LoadAsString($"SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.{TicketId}.comments.json")
                .DeserialiseZendesk<CommentResponse>()
                .ToResult();

        public Task<TicketResponse> GetTicketWithSideloads([Path] long id, SideLoads include)
            => GetTicket(id);

        public Task<TicketResponse> PostTicket([Body] TicketRequest ticket)
            => GetTicket(ticket.Ticket.Id);

        public Task UpdateTicket([Path] long id, [Body] TicketRequest ticket)
            => Task.CompletedTask;

        public Task<SearchResponse> SearchTickets([Query] string query)
            => throw new NotImplementedException();

        public Task UpdateTags([Path] long id, [Body] SafeModifyTags update)
            => Task.CompletedTask;

        public Task<TicketFieldResponse> GetTicketFieldIds() =>
            Task.FromResult(new TicketFieldResponse { TicketFields = new TicketField[0] });
    }

    internal static class ZendeskStringExtensions
    {
        internal static T DeserialiseZendesk<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json, ApiFactoryExtensions.serialiser);

        internal static Task<T> ToResult<T>(this T t)
            => Task.FromResult(t);
    }
}
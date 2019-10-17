using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class SharingTickets : ISharingTickets
    {
        const string pendingTag = "pending_middleware";
        const string sendingTag = "sending_middleware";

        private readonly string[] Tags = new[]
        {
            pendingTag, 
            sendingTag,
        };

        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<Option<TicketResponse>> GetTicketForSharing(long id)
        {
            var response = await api.GetTicketWithRequiredSideloads(id);

            if (!TicketContainsSharingTag(response.Ticket)) return default;

            response.Comments = (await api.GetTicketComments(id)).Comments;
            return response;
        }

        private bool TicketContainsSharingTag(Ticket ticket)
        {
            return ticket.Tags.Intersect(Tags).Any();
        }

        public async Task<long[]> GetTicketsForSharing()
        {
            var search = string.Join(" ", Tags.Select(x => $"tags:{x}"));
            var response = await api.SearchTickets(search);
            return response?.Results?
                .Where(TicketContainsSharingTag)
                .Select(x => x.Id).ToArray()
                ?? new long[] { };
        }

        public Task MarkSharing(Ticket t)
        {
            t.Tags.Remove(pendingTag);
            t.Tags.Add(sendingTag);
            return api.PutTicket(t);
        }

        public Task MarkShared(Ticket t)
        {
            t.Tags.Remove(sendingTag);
            return api.PutTicket(t);
        }
    }
}
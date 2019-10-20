using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public enum SharingReason
    {
        Unknown,
        Solved,
        Escalated,
    }

    public class SharingTickets : ISharingTickets
    {
        const string pendingTag = "pending_middleware";
        const string sendingTag = "sending_middleware";

        private readonly string[] Tags = new[]
        {
            $"{pendingTag}_{SharingReason.Solved.ToString().ToLower()}", 
            $"{sendingTag}_{SharingReason.Solved.ToString().ToLower()}", 
            $"{pendingTag}_{SharingReason.Escalated.ToString().ToLower()}", 
            $"{sendingTag}_{SharingReason.Escalated.ToString().ToLower()}", 
        };

        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<Option<(TicketResponse, SharingReason)>> GetTicketForSharing(long id)
        {
            var response = await api.GetTicketWithRequiredSideloads(id);

            return await 
                GetReasonForSharing(response.Ticket)
                .ToAsync()
                .MapAsync(FillOutResponse)
                .ToOption();


            async Task<(TicketResponse, SharingReason x)> FillOutResponse(SharingReason reason)
            {
                response.Comments = (await api.GetTicketComments(response.Ticket.Id)).Comments;
                return (response, reason);
            }
        }

        private Option<SharingReason> GetReasonForSharing(Ticket ticket)
        {
            return GetSharingTagsInTicket(ticket)
                .Select(SegmentAfterLastUnderscore)
                .Select(TryParseReason)
                .FirstOrDefault();

            string SegmentAfterLastUnderscore(string tag) =>
                tag.Split('_').LastOrDefault() ?? "";

            Option<SharingReason> TryParseReason(string reason) =>
                Enum.TryParse<SharingReason>(reason, true, out var r) ? Option<SharingReason>.Some(r) : default;
        }

        private IEnumerable<string> GetSharingTagsInTicket(Ticket ticket) =>
            ticket.Tags.Intersect(Tags);

        public async Task<long[]> GetTicketsForSharing()
        {
            var search = string.Join(" ", Tags.Select(x => $"tags:{x}"));
            var response = await api.SearchTickets(search);
            return response?.Results?
                .Where(TicketContainsSharingTag)
                .Select(x => x.Id).ToArray()
                ?? new long[] { };
        }

        private bool TicketContainsSharingTag(Ticket ticket) =>
            GetSharingTagsInTicket(ticket).Any();

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
using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SFA.DAS.Zendesk.Monitor.Zendesk.EnumValues;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class SharingTickets : ISharingTickets
    {
        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<Option<SharedTicket>> GetTicketForSharing(long id)
        {
            var response = await api.GetTicketWithRequiredSideloads(id);
            var comments = await api.GetTicketComments(response.Ticket);
            var audits = await api.GetTicketAudits(response.Ticket);
            return await SharedTicket.Create(response, comments, audits).ToOption();
        }

        public async Task<long[]> GetTicketsForSharing()
        {
            var search = string.Join(" ", AllSharingTags.Select(x => $"tags:{x}"));
            var response = await api.SearchTickets(search);
            return response?.Results?
                .Where(TicketContainsSharingTag)
                .Select(x => x.Id).ToArray()
                ?? Array.Empty<long>();
        }

        private bool TicketContainsSharingTag(Ticket ticket) =>
            GetSharingTagsInTicket(ticket).Any();

        private static IEnumerable<string> GetSharingTagsInTicket(Ticket ticket) =>
            ticket.Tags.Intersect(AllSharingTags);

        public Task MarkSharing(SharedTicket share)
        {
            if (share == null) throw new ArgumentNullException(nameof(share));
            return MarkSharing(share.Response.Ticket, share.Reason);
        }

        private Task MarkSharing(Ticket ticket, SharingReason reason)
            => api.ModifyTags(
                ticket,
                additions: new[] { MakeTag(SharingState.Sending, reason) },
                removals: new[] { MakeTag(SharingState.Pending, reason) }
                             );

        public Task MarkShared(SharedTicket share)
        {
            if (share == null) throw new ArgumentNullException(nameof(share));
            return MarkShared(share.Response.Ticket, share.Reason);
        }

        private Task MarkShared(Ticket ticket, SharingReason reason)
            => api.ModifyTags(
                ticket,
                removals: new[] { MakeTag(SharingState.Sending, reason) });

        private enum SharingState
        {
            Pending,
            Sending,
        }

        private static readonly string[] AllSharingTags =
            CartesianProduct
                .Of(ListEnum<SharingState>(), SharingReason.List)
                .Using(MakeTag).ToArray();

        private static string MakeTag(SharingState state, SharingReason reason) =>
            $"{state}_{reason.AsTag()}".ToLower();
    }
}
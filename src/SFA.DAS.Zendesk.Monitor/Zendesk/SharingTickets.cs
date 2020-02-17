using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return await SharedTicket.Create(response, LoadComments).ToOption();
        }

        private Func<TicketResponse, Task<TicketResponse>> LoadComments
            => async response
            =>
            {
                var comments = await api.GetTicketComments(response.Ticket);
                var audits = await api.GetTicketAudits(response.Ticket);
                response.Comments = TaggedComments(comments, audits);
                return response;
            };

        private static Comment[] TaggedComments(Comment[] comments, Audit[] audits)
        {
            if (comments == null || audits == null)
                return Array.Empty<Comment>();

            var taggedAudits = audits
                .Where(a => a.Events.Any(IsEscalationEvent));

            var privateComments = taggedAudits
                .SelectMany(a => a.Events)
                .Where(e => e.Type == "Comment" && e.Public != true)
                .Select(e => e.Id);

            var taggedComments = comments
                .Where(x => privateComments.Contains(x.Id));

            return taggedComments.ToArray();

            static bool IsEscalationEvent(Event e)
                => e.Type == "Change" &&
                   e.Value?.Contains("escalated_tag") == true;
        }

        private static IEnumerable<string> GetSharingTagsInTicket(Ticket ticket) =>
            ticket.Tags.Intersect(AllSharingTags);

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

        public Task MarkSharing(SharedTicket share)
        {
            if (share == null) throw new ArgumentNullException(nameof(share));
            return MarkSharing(share.Response.Ticket, share.Reason);
        }

        private Task MarkSharing(Ticket ticket, SharingReason reason)
            => api.ModifyTags(
                ticket,
                additions: new[] { MakeTag(SharingState.Sending, reason) },
                removals:  new[] { MakeTag(SharingState.Pending, reason) }
                );

        public Task MarkShared(SharedTicket share)
        {
            if (share == null) throw new ArgumentNullException(nameof(share));
            return MarkShared(share.Response.Ticket, share.Reason);
        }

        private Task MarkShared(Ticket ticket, SharingReason reason)
            => api.ModifyTags(
                ticket,
                removals: new[] { MakeTag(SharingState.Sending, reason) }
                );

        private enum SharingState
        {
            Pending,
            Sending,
        }

        private static readonly string[] AllSharingTags = new[]
        {
            MakeTag(SharingState.Pending, SharingReason.Solved),
            MakeTag(SharingState.Sending, SharingReason.Solved),
            MakeTag(SharingState.Pending, SharingReason.Escalated),
            MakeTag(SharingState.Sending, SharingReason.Escalated),
        };

        private static string MakeTag(SharingState state, SharingReason reason) =>
            $"{state}_middleware_{reason}".ToLower();
    }
}
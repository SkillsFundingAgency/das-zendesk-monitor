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

            var sharing = ReasonForSharing(response);

            await sharing.IfSomeAsync(LoadComments);

            sharing = sharing.Filter(t => t.Response.Comments.Any());

            return sharing;
        }

        private async Task LoadComments(SharedTicket response)
        {
            var comments = await api.GetTicketComments(response.Response.Ticket);
            response.Response.Comments =
                FilterComments(comments, response.Response.Audits);
        }

        private static Comment[] FilterComments(Comment[] comments, Audit[] audits)
        {
            if (comments == null || audits == null)
                return Array.Empty<Comment>();

            var taggedAudits = audits
                .Where(a => a.Events.Any(IsEscalationEvent));

            var privateComments = taggedAudits
                .SelectMany(a => a.Events)
                .Where(e => e.Type == TypeEnum.Comment && e.Public != true)
                .Select(e => e.Id);

            return comments
                .Where(x => privateComments.Contains(x.Id))
                .ToArray();

            static bool IsEscalationEvent(Event e)
                => e.Type == TypeEnum.Change &&
                   e.Value?.Contains("escalated_tag") == true;
        }

        private Option<SharedTicket> ReasonForSharing(TicketResponse response)
        {
            return GetSharingTagsInTicket(response.Ticket)
                .Select(SegmentAfterLastUnderscore)
                .Select(TryParseReason)
                .FirstOrDefault();

            string SegmentAfterLastUnderscore(string tag) =>
                tag.Split('_').LastOrDefault() ?? "";

            Option<SharedTicket> TryParseReason(string reason) =>
                Enum.TryParse<SharingReason>(reason, true, out var r) 
                    ? new SharedTicket(r, response)
                    : Option<SharedTicket>.None;
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

        private Task MarkSharing(Ticket t, SharingReason reason)
        {
            t.RemoveTag(MakeTag(SharingState.Pending, reason));
            t.AddTag(MakeTag(SharingState.Sending, reason));
            return api.PutTicket(t);
        }

        public Task MarkShared(SharedTicket share)
        {
            if (share == null) throw new ArgumentNullException(nameof(share));
            return MarkShared(share.Response.Ticket, share.Reason);
        }

        private Task MarkShared(Ticket t, SharingReason reason)
        {
            t.RemoveTag(MakeTag(SharingState.Sending, reason));
            return api.PutTicket(t);
        }

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
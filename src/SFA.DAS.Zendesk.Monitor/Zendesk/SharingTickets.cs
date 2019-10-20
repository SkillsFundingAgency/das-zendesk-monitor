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
        private readonly IApi api;

        public SharingTickets(IApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<Option<(TicketResponse, SharingReason)>> GetTicketForSharing(long id)
        {
            var response = await api.GetTicketWithRequiredSideloads(id);

            return await
                ReasonForSharing(response.Ticket)
                .ToAsync()
                .MapAsync(FillOutResponse)
                .ToOption();

            async Task<(TicketResponse, SharingReason x)> FillOutResponse(SharingReason reason)
            {
                response.Comments = (await api.GetTicketComments(response.Ticket.Id)).Comments;
                return (response, reason);
            }
        }

        private Option<SharingReason> ReasonForSharing(Ticket ticket)
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
            ticket.Tags.Intersect(AllSharingTags);

        public async Task<long[]> GetTicketsForSharing()
        {
            var search = string.Join(" ", AllSharingTags.Select(x => $"tags:{x}"));
            var response = await api.SearchTickets(search);
            return response?.Results?
                .Where(TicketContainsSharingTag)
                .Select(x => x.Id).ToArray()
                ?? new long[] { };
        }

        private bool TicketContainsSharingTag(Ticket ticket) =>
            GetSharingTagsInTicket(ticket).Any();

        public Task MarkSharing(Ticket t, SharingReason reason)
        {
            t.Tags.Remove(MakeTag(SharingState.Pending, reason));
            t.Tags.Add(MakeTag(SharingState.Sending, reason));
            return api.PutTicket(t);
        }

        public Task MarkShared(Ticket t, SharingReason reason)
        {
            t.Tags.Remove(MakeTag(SharingState.Sending, reason));
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
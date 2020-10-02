using LanguageExt;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SFA.DAS.Zendesk.Monitor.Zendesk.SmartEnumValues;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public sealed class SharedTicket
    {
        public long Id { get; }

        public SharingReason Reason { get; }

        public TicketResponse Response { get; }

        internal static OptionAsync<SharedTicket> Create(
            TicketResponse response,
            Comment[] comments,
            Audit[] audits)
        {
            _ = response ?? throw new ArgumentNullException(nameof(response));
            _ = comments ?? throw new ArgumentNullException(nameof(comments));
            _ = audits ?? throw new ArgumentNullException(nameof(audits));

            return ReasonForSharing(response, comments, audits);
        }

        private static OptionAsync<SharedTicket> ReasonForSharing(
            TicketResponse response,
            Comment[] comments,
            Audit[] audits)
        {
            var shareReason =
                GetSharingTagsInTicket(response.Ticket).ToOption()
                .Map(LastWord)
                .Bind(ParseIgnoringCase<SharingReason>).ToAsync();

            return
                from reason in shareReason
                from responseWithComments in shareReason.Map(AddComments)
                where TicketWasShared(responseWithComments, reason)
                select new SharedTicket(reason, responseWithComments);

            static string LastWord(string tag)
                => tag?.Split('_').LastOrDefault() ?? "";

            TicketResponse AddComments(SharingReason reason)
                => reason.AddCommentsToResponse(response, comments, audits);
        }

        private static IEnumerable<string> GetSharingTagsInTicket(Ticket ticket)
            => ticket.Tags.Where(TagEndsWithSharingReason);

        private static bool TagEndsWithSharingReason(string tag) =>
            SharingReason.List.Any(reason => tag.EndsWith(reason.AsTag()));

        private static bool TicketWasShared(TicketResponse response, SharingReason reason)
            => reason == SharingReason.Solved
            || reason == SharingReason.HandedOff
            || (reason == SharingReason.Escalated && response.Comments.Any());

        private SharedTicket(SharingReason reason, TicketResponse response)
        {
            Id = response.Ticket?.Id
                ?? throw new ArgumentException("Response does not contain the Ticket ID");

            Reason = reason;
            Response = response;
        }

        internal T Switch<T>(Func<bool, T> solved, Func<bool, T> handedOff, Func<bool, T> escalated)
        {
            return Reason switch
            {
                var e when e == SharingReason.Solved => solved(true),
                var e when e == SharingReason.Escalated => escalated(true),
                var e when e == SharingReason.HandedOff => handedOff(true),
                _ => throw new InvalidOperationException("Undeclared SharingReason found in Switch"),
            };
        }
    }
}
using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public sealed class SharedTicket
    {
        public long Id { get; }

        public SharingReason Reason { get; }

        public TicketResponse Response { get; }

        public static OptionAsync<SharedTicket> Create(
            TicketResponse response,
            Func<TicketResponse, Task<TicketResponse>> loadComments)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (loadComments == null) throw new ArgumentNullException(nameof(response));

            return ReasonForSharing(response, loadComments);
        }

        private static OptionAsync<SharedTicket> ReasonForSharing(
            TicketResponse response,
            Func<TicketResponse, Task<TicketResponse>> loadComments)
        {
            var shareReason =
                GetSharingTagsInTicket(response.Ticket).ToOption()
                .Map(LastWord)
                .Bind(parseEnumIgnoreCase<SharingReason>).ToAsync();

            return
                from reason in shareReason
                from responseWithComments in shareReason.MapAsync(_ => loadComments(response))
                where TicketWasShared(responseWithComments, reason)
                select new SharedTicket(reason, responseWithComments);

            static string LastWord(string tag)
                => tag?.Split('_').LastOrDefault() ?? "";
        }

        private static IEnumerable<string> GetSharingTagsInTicket(Ticket ticket)
            => ticket.Tags.Where(
                t => t.EndsWith(SharingReason.Solved.AsTag())
                  || t.EndsWith(SharingReason.Escalated.AsTag()));

        private static bool TicketWasShared(TicketResponse response, SharingReason reason)
            => reason == SharingReason.Solved
            || (reason == SharingReason.Escalated && response.Comments.Any());

        private SharedTicket(SharingReason reason, TicketResponse response)
        {
            if (reason > SharingReason.Escalated)
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undeclared SharingReason found.");

            Id = response.Ticket?.Id
                ?? throw new ArgumentException("Response does not contain the Ticket ID");

            Reason = reason;
            Response = response;
        }

        internal T Switch<T>(Func<bool, T> solved, Func<bool, T> escalated)
        {
            return Reason switch
            {
                SharingReason.Solved => solved(true),
                SharingReason.Escalated => escalated(true),
                _ => throw new InvalidOperationException("Undeclared SharingReason found in Switch"),
            };
        }
    }
}
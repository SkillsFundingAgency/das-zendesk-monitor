using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public sealed class SharedTicket
    {
        public long Id { get; }

        internal SharingReason Reason { get; }

        public TicketResponse Response { get; }
        public string ReasonTag => Reason.AsTag();

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
            response.Comments = comments;
            response.Audits = audits;

            return SharingReasonValidations.List
                .Bind(r => r.TryShareTicket(response))
                .ToOption()
                .ToAsync();
        }

        internal SharedTicket(SharingReason reason, TicketResponse response)
        {
            Id = response.Ticket?.Id
                ?? throw new ArgumentException("Response does not contain the Ticket ID");

            this.Reason = reason;
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
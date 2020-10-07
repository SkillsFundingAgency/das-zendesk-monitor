using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal static class TicketExtensions
    {
        internal static bool IsSharedBecause(this TicketResponse response, SharingReason reason) =>
            response.Ticket.Tags.Any(t =>
                t.EndsWith(reason.AsTag(), StringComparison.CurrentCultureIgnoreCase));

        internal static TicketResponse WithComments(this TicketResponse response, Comment[] comments) =>
            new TicketResponse
            {
                Ticket = response.Ticket,
                Users = response.Users,
                Organizations = response.Organizations,
                Audits = response.Audits,
                Comments = comments,
            };

        internal static TicketResponse WithoutComments(this TicketResponse response) =>
            response.WithComments(Array.Empty<Comment>());
    }
}
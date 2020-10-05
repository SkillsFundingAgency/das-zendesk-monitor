using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;
using static LanguageExt.Prelude;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal class EscalatedResponseValidation : ISharingValidation
    {
        public SharingReason Reason => SharingReason.Escalated;

        public Option<SharedTicket> TryShareTicket(TicketResponse response)
        {
            var escalatedComments = TaggedComments(response.Comments, response.Audits);

            return response.IsSharedBecause(Reason) && escalatedComments.Any()
                ? Some(new SharedTicket(Reason, response.WithComments(escalatedComments)))
                : None;
        }

        private static Comment[] TaggedComments(Comment[] comments, Audit[] audits)
        {
            if (comments == null || audits == null)
                return System.Array.Empty<Comment>();

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
    }
}
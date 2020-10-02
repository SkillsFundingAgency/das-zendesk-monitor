using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public partial class SharingReason
    {
        private class EscalatedReason : SharingReason
        {
            public EscalatedReason(string name, int value) : base(name, value)
            {
            }

            public override TicketResponse AddCommentsToResponse(TicketResponse response, Comment[] comments, Audit[] audits)
            {
                response.Comments = TaggedComments(comments, audits);
                return response;
            }

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
        }
    }
}
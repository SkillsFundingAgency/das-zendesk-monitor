using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class AuditedComment : Comment
    {
        public AuditedComment(Audit audit)
        {
            AsAudit = audit;
            AddCommentToAudit(AsAudit);
        }

        public Comment AsComment => this;

        public Audit AsAudit { get; }

        public Event AuditTagEvent => AsAudit.Events[0];

        public new long? Id
        {
            get => base.Id;
            set => base.Id = AsAudit.Events[^1].Id = value;
        }
        public new bool? Public
        {
            get => base.Public;
            set => base.Public = AsAudit.Events[^1].Public = value;
        }
        public new string Body
        {
            get => base.Body;
            set => base.Body = AsAudit.Events[^1].Body = value;
        }

        public void Share()
        {
            Public = false;

            AuditTagEvent.Type = TypeEnum.Change;
            AuditTagEvent.FieldName = "tags";
            AuditTagEvent.Value = "ignored_tag, escalated_tag";
            AuditTagEvent.PreviousValue = "ignored_tag";
        }

        private void AddCommentToAudit(Audit audit)
        {
            var auditEvent = new Event
            {
                Id = base.Id,
                Body = base.Body,
                Public = base.Public,
                Attachments = base.Attachments,
                Type = TypeEnum.Comment,
            };

            var events = audit.Events;
            Array.Resize(ref events, events.Length + 1);
            events[^1] = auditEvent;
            audit.Events = events;
        }
    }
}

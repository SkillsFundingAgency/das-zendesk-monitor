using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public partial class Comment
    {
        public long? Id { get; set; }

        public string Type { get; set; }

        public long? AuthorId { get; set; }

        public string Body { get; set; }

        public string HtmlBody { get; set; }

        public string PlainBody { get; set; }

        public bool? Public { get; set; }

        public Attachment[] Attachments { get; set; }

        public Via Via { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    }
}
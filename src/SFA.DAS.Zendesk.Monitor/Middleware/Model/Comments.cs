using System;

namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class Comments
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Attachment[] Attachments { get; set; }
    }
}
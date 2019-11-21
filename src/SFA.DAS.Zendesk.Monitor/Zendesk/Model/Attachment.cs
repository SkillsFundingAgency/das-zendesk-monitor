#nullable disable
using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class Attachment
    {
        public long? Id { get; set; }
        public string FileName { get; set; }
        public Uri ContentUrl { get; set; }
        public string ContentType { get; set; }
        public long? Size { get; set; }
    }
}
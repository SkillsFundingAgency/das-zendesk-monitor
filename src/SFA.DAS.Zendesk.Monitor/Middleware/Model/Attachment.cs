#nullable disable
using System;

namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class Attachment
    {
        public string Filename { get; set; }
        public Uri Url { get; set; }
    }
}
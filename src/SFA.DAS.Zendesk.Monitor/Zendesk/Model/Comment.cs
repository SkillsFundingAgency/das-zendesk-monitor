#nullable disable
using Newtonsoft.Json;
using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    public class Comment
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

    public class Audit
    {
        public long Id { get; set; }

        public long TicketId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public long AuthorId { get; set; }

        //public Metadata Metadata { get; set; }

        public Event[] Events { get; set; }

        public Via Via { get; set; }
    }

    public class Event
    {
        public long? Id { get; set; }
        public string Type { get; set; }
        public Via Via { get; set; }
        public long? AuthorId { get; set; }
        public string Body { get; set; }
        public bool? Public { get; set; }
        public long? AuditId { get; set; }
        public long[] Recipients { get; set; }
        public string HtmlBody { get; set; }
        public string PlainBody { get; set; }
        public object[] Attachments { get; set; }

        [JsonConverter(typeof(EventValueJsonConverter))]
        public string Value { get; set; }
        
        public string FieldName { get; set; }
        public object PreviousScheduleId { get; set; }
        public string NewScheduleId { get; set; }
        [JsonConverter(typeof(EventValueJsonConverter))]
        public string PreviousValue { get; set; }
        public string Resource { get; set; }
        public string Subject { get; set; }
        public string MacroTitle { get; set; }
        public string MacroId { get; set; }
        public bool? MacroDeleted { get; set; }
    }


    //public class Metadata
    //{
    //    public SystemClass System { get; set; }
    //    public Custom Custom { get; set; }
    //}

    public class Custom
    {
    }

    public class SystemClass
    {
        public string Client { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
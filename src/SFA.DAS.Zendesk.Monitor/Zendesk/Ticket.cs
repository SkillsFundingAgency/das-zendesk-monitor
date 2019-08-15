using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public partial class Empty
    {
        public Ticket Ticket { get; set; }
    }

    public partial class Ticket
    {
        public Uri Url { get; set; }

        public long Id { get; set; }

        public Comment Comment { get; set; }

        public object ExternalId { get; set; }

        public Via Via { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string Type { get; set; }

        public string Subject { get; set; }

        public string RawSubject { get; set; }

        public string Description { get; set; }

        public string Priority { get; set; }

        public string Status { get; set; }

        public object Recipient { get; set; }

        public long? RequesterId { get; set; }

        public long? SubmitterId { get; set; }

        public object AssigneeId { get; set; }

        public object OrganizationId { get; set; }

        public long? GroupId { get; set; }

        public object[] CollaboratorIds { get; set; }

        public object[] FollowerIds { get; set; }

        public object[] EmailCcIds { get; set; }

        public object ForumTopicId { get; set; }

        public object ProblemId { get; set; }

        public bool? HasIncidents { get; set; }

        public bool? IsPublic { get; set; }

        public object DueAt { get; set; }

        public List<string> Tags { get; set; }

        public Field[] CustomFields { get; set; }

        public SatisfactionRating SatisfactionRating { get; set; }

        public object[] SharingAgreementIds { get; set; }

        public Field[] Fields { get; set; }

        public object[] FollowupIds { get; set; }

        public long? TicketFormId { get; set; }

        public long? BrandId { get; set; }

        public object SatisfactionProbability { get; set; }

        public bool? AllowChannelback { get; set; }

        public bool? AllowAttachments { get; set; }
    }

    public partial class Comment
    {
        public long? Id { get; set; }

        public string Type { get; set; }

        public long? AuthorId { get; set; }

        public string Body { get; set; }

        public string HtmlBody { get; set; }

        public string PlainBody { get; set; }

        public bool? Public { get; set; }

        public object[] Attachments { get; set; }

        public Via Via { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    }

    public partial class Field
    {
        public long Id { get; set; }

        public object Value { get; set; }
    }

    public partial class SatisfactionRating
    {
        public string Score { get; set; }
    }

    public partial class Via
    {
        public string Channel { get; set; }

        public Source Source { get; set; }
    }

    public partial class Source
    {
        public From From { get; set; }

        public From To { get; set; }

        public object Rel { get; set; }
    }

    public partial class From
    {
        public string Address { get; set; }

        public string Name { get; set; }

        public string[] OriginalRecipients { get; set; }
    }

    public partial class To
    {
        public string Name { get; set; }

        public string Address { get; set; }
    }

    public partial class Empty
    {
        public static Empty FromJson(string json) => JsonConvert.DeserializeObject<Empty>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Empty self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
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

    public partial class Group
    {
        public Uri Url { get; set; }

        public long? Id { get; set; }

        public string Name { get; set; }

        public bool? Deleted { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public partial class User
    {
        public long? Id { get; set; }

        public Uri Url { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string TimeZone { get; set; }

        public string IanaTimeZone { get; set; }

        public object Phone { get; set; }

        public object SharedPhoneNumber { get; set; }

        public object Photo { get; set; }

        public long? LocaleId { get; set; }

        public string Locale { get; set; }

        public object OrganizationId { get; set; }

        public string Role { get; set; }

        public bool? Verified { get; set; }

        public object ExternalId { get; set; }

        public object[] Tags { get; set; }

        public object Alias { get; set; }

        public bool? Active { get; set; }

        public bool? Shared { get; set; }

        public bool? SharedAgent { get; set; }

        public object LastLoginAt { get; set; }

        public bool? TwoFactorAuthEnabled { get; set; }

        public object Signature { get; set; }

        public object Details { get; set; }

        public object Notes { get; set; }

        public object RoleType { get; set; }

        public object CustomRoleId { get; set; }

        public bool? Moderator { get; set; }

        public string TicketRestriction { get; set; }

        public bool? OnlyPrivateComments { get; set; }

        public bool? RestrictedAgent { get; set; }

        public bool? Suspended { get; set; }

        public bool? ChatOnly { get; set; }

        public object DefaultGroupId { get; set; }

        public bool? ReportCsv { get; set; }

        public UserFields UserFields { get; set; }
    }

    public partial class UserFields
    {
        public object AddressLine1 { get; set; }

        public object AddressLine2 { get; set; }

        public object AddressLine3 { get; set; }

        public object City { get; set; }

        public object ContactType { get; set; }

        public object County { get; set; }

        public object Postcode { get; set; }
    }
}
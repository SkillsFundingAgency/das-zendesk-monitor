using System;
using System.Collections.Generic;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
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
}
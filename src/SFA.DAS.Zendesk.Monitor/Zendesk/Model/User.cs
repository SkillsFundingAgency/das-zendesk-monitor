#nullable disable

using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
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
}
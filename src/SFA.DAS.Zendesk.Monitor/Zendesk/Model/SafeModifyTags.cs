using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.Zendesk.Model
{
    /// <summary>
    /// Update tags of a ticket without fear of overwriting concurrent changes
    /// made by other processes.
    /// <see cref="https://github.com/SkillsFundingAgency/das-zendesk-monitor/issues/32"/>
    /// <see cref="https://developer.zendesk.com/rest_api/docs/support/tickets#protect-against-ticket-update-collisions"/>
    /// </summary>
    [JsonConverter(typeof(SafeModifyTagsConverter))]
    public sealed class SafeModifyTags
    {
        public SafeModifyTags(Ticket current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            UpdatedStamp = current?.UpdatedAt
                ?? throw new ArgumentNullException(nameof(Ticket.UpdatedAt));

            Tags = current?.Tags
                ?? throw new ArgumentNullException(nameof(Ticket.Tags));
        }

        public bool SafeUpdate { get; } = true;

        public DateTimeOffset UpdatedStamp { get; }

        public List<string> Tags { get; }

        public void Add(params string[] tags)
            => Tags.AddRange(tags ?? Array.Empty<string>());

        public void Remove(params string[] tags)
            => Tags.RemoveAll(x => tags.Contains(x));
    }
}
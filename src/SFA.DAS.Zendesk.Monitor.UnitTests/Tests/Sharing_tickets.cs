using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixtureCustomisation;
using SFA.DAS.Zendesk.Monitor.UnitTests.Helpers;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class Sharing_tickets
    {
        [Theory, AutoData]
        public void AuditedCommentMapping(AuditedComment comment)
        {
            comment.Id.Should().Be(comment.AsAudit.Events[^1].Id);
            comment.Public.Should().Be(comment.AsAudit.Events[^1].Public);
            comment.Body.Should().Be(comment.AsAudit.Events[^1].Body);
        }

        [Theory, ZendeskAutoData]
        public async Task Sends_previously_failed_solved_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Solved] Ticket ticket)
        {
            ticket.Tags = new List<string> { "sending_middleware_solved" };
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, ZendeskAutoData]
        public async Task Sends_previously_failed_escalated_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Escalated] Ticket ticket)
        {
            ticket.Tags = new List<string> { "sending_middleware_escalated" };
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().EscalateTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }
    }
}
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixtureCustomisation;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class Solving_tickets
    {
        [Theory, ZendeskAutoData]
        public async Task Sends_ticket_to_middleware(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            var expectedTicket = new
            {
                Ticket = new
                {
                    ticket.Id,
                    ticket.Status,
                    ticket.Description,
                    ticket.Subject,
                    ticket.CreatedAt,
                }
            };

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(expectedTicket)));
        }

        [Theory, ZendeskAutoData]
        public void Marks_ticket_as_sharing_before_sending_to_middleware(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            middleware.When(x => x.SolveTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(_ => throw new Exception("Stop test at Middleware step"));

            sut.Invoking(s => s.ShareTicket(ticket.Id))
               .Should().ThrowAsync<Exception>()
               .WithMessage("Stop test at Middleware step");

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware_solved".ToLower())
                .And.Contain("sending_middleware_solved".ToLower());
        }

        [Theory, ZendeskAutoData]
        public async Task Sends_ticket_to_middleware_with_requester(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved] Ticket ticket,
            User reporter)
        {
            ticket.RequesterId = reporter.Id;
            zendesk.Tickets.Add(ticket);
            zendesk.Users.Add(reporter);

            await sut.ShareTicket(ticket.Id);

            var mwt = new
            {
                Ticket = new
                {
                    Requester = new
                    {
                        reporter.Id,
                        reporter.Name,
                        reporter.Email,
                        reporter.Phone,
                        UserFields = new
                        {
                            reporter.UserFields.AddressLine1,
                            reporter.UserFields.AddressLine2,
                            reporter.UserFields.AddressLine3,
                            reporter.UserFields.City,
                            reporter.UserFields.County,
                            reporter.UserFields.Postcode,
                        }
                    }
                }
            };

            await middleware.Received().SolveTicket(Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(mwt)));
        }

        [Theory, ZendeskAutoData]
        public async Task Sends_ticket_to_middleware_with_organisation(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved] Ticket ticket,
            Organization org)
        {
            ticket.OrganizationId = org.Id;
            zendesk.Tickets.Add(ticket);
            zendesk.Organizations.Add(org);

            await sut.ShareTicket(ticket.Id);

            var mwt = new
            {
                Ticket = new
                {
                    Organization = new
                    {
                        org.Id,
                        org.Name,
                        OrganizationFields = new
                        {
                            org.OrganizationFields.MainPhone,
                            org.OrganizationFields.AddressLine1,
                            org.OrganizationFields.AddressLine2,
                            org.OrganizationFields.AddressLine3,
                            org.OrganizationFields.City,
                            org.OrganizationFields.County,
                            org.OrganizationFields.Postcode,
                            org.OrganizationFields.OrganisationStatus,
                            org.OrganizationFields.OrganisationType,
                        }
                    }
                }
            };

            await middleware.Received().SolveTicket(Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(mwt)));
        }

        [Theory, ZendeskAutoData]
        public async Task Marks_ticket_as_shared(
            [Frozen] FakeZendeskApi zendesk,
            Watcher sut,
            [Pending.Solved] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware_solved")
                .And.NotContain("sending_middleware_solved");
        }

        [Theory, ZendeskAutoData]
        public async Task Marks_ticket_as_shared_with_duplicate_tags(
            [Frozen] FakeZendeskApi zendesk,
            Watcher sut,
            [Pending.Solved] Ticket ticket)
        {
            ticket.Tags.Add("sending_middleware_solved");
            ticket.Tags.Add("pending_middleware_solved");
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware_solved")
                .And.NotContain("sending_middleware_solved");
        }

        [Theory, ZendeskAutoData]
        public async Task Ignores_tickets_without_sharing_tags(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            Ticket ticket)
        {
            ticket.Tags.Clear();
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.DidNotReceive().SolveTicket(Arg.Any<Middleware.EventWrapper>());
        }

        [Theory, ZendeskAutoData]
        public async Task Sends_ticket_to_middleware_with_no_comments(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved] Ticket ticket,
            Comment[] comments)
        {
            // Given
            zendesk.Tickets.Add(ticket);
            zendesk.AddComments(ticket, comments);

            // When
            await sut.ShareTicket(ticket.Id);

            // Then
            var mwt = new
            {
                Ticket = new
                {
                    Comments = Array.Empty<Middleware.Model.Comments>(),
                }
            };

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(
                    body => body.Should().BeEquivalentTo(mwt)));
        }
    }
}
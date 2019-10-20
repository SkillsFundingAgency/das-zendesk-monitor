using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixture;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class Sharing_tickets
    {
        [Theory, AutoDataDomain]
        public async Task Marks_ticket_as_sharing_before_sending_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);
            middleware.When(x => x.SolveTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(x => { throw new Exception("Stop test at Middleware step"); });

            try
            {
                await sut.ShareTicket(ticket.Id);
            }
            catch { }

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware")
                .And.Contain("sending_middleware");
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket)
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

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_comments([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket, Comment[] comments)
        {
            zendesk.Tickets.Add(ticket);
            zendesk.AddComments(ticket, comments);

            await sut.ShareTicket(ticket.Id);

            var mwt = new
            {
                Ticket = new
                {
                    Comments = comments.Select(c =>
                    new
                    {
                        c.Id,
                        c.Body,
                        c.CreatedAt,
                    }),
                }
            };

            await middleware.Received().SolveTicket(Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(mwt)));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_requester([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket, User reporter)
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

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_organisation([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket, Organization org)
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
                            org.OrganizationFields.AddressLine1,
                            org.OrganizationFields.AddressLine2,
                            org.OrganizationFields.AddressLine3,
                            org.OrganizationFields.City,
                            org.OrganizationFields.County,
                            org.OrganizationFields.Postcode,
                        }
                    }
                }
            };

            await middleware.Received().SolveTicket(Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(mwt)));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_previously_failed_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending] Ticket ticket)
        {
            ticket.Tags.Remove("pending_middleware");
            ticket.Tags.Add("sending_middleware");
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Marks_ticket_as_shared([Frozen] FakeZendeskApi zendesk, Watcher sut, [Pending] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware")
                .And.NotContain("sending_middleware");
        }

        [Theory, AutoDataDomain]
        public async Task Ignores_tickets_without_sharing_tags([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, Ticket ticket)
        {
            ticket.Tags.Clear();
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.DidNotReceive().SolveTicket(Arg.Any<Middleware.EventWrapper>());
        }

        private class AutoDataDomainAttribute : AutoDataAttribute
        {
            public AutoDataDomainAttribute() : base(() => Customise())
            {
            }

            private static IFixture Customise()
            {
                var fixture = new Fixture();
                fixture.Register<IApi>(() => fixture.Create<FakeZendeskApi>());
                fixture.Register<ISharingTickets>(() => fixture.Create<SharingTickets>());
                fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
                return fixture;
            }
        }
    }
}
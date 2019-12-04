 using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixture;
using SFA.DAS.Zendesk.Monitor.Zendesk;
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
        public static IEnumerable<object[]> Tags()
        {
            yield return new object[] { As.Escalated };
            yield return new object[] { As.Solved };
        }

        [Theory, MemberAutoDomainData(nameof(Tags))]
        public async Task Marks_ticket_as_sharing_before_sending_to_middleware(As state, [Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, Ticket ticket)
        {
            ticket.Tags.Add($"pending_middleware_{state}".ToLower());
            zendesk.Tickets.Add(ticket);
            middleware.When(x => x.SolveTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(x => { throw new Exception("Stop test at Middleware step"); });
            middleware.When(x => x.EscalateTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(x => { throw new Exception("Stop test at Middleware step"); });

            try
            {
                await sut.ShareTicket(ticket.Id);
            }
            catch { /* Expecting the middleware to fail */ }

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain($"pending_middleware_{state}".ToLower())
                .And.Contain($"sending_middleware_{state}".ToLower());
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Solved)] Ticket ticket)
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
        public async Task Sends_ticket_to_correct_endpoint([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Escalated)] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().EscalateTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_comments([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Solved)] Ticket ticket, Comment[] comments)
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
        public async Task Sends_ticket_to_middleware_with_attachments([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Solved)] Ticket ticket, Comment[] comments)
        {
            zendesk.Tickets.Add(ticket);
            zendesk.AddComments(ticket, comments);

            await sut.ShareTicket(ticket.Id);

            var expected = new
            {
                Ticket = new
                {
                    Comments = comments.Select(c =>
                    new
                    {
                        c.Id,
                        Attachments = c.Attachments.Select(a => new
                        {
                            Filename = a.FileName,
                            Url = a.ContentUrl,
                        }),
                    }),
                }
            };

            await middleware.Received().SolveTicket(Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(expected)));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_requester([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Solved)] Ticket ticket, User reporter)
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
        public async Task Sends_ticket_to_middleware_with_organisation([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending(As.Solved)] Ticket ticket, Organization org)
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
        public async Task Sends_previously_failed_solved_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, Ticket ticket)
        {
            ticket.Tags.Add("sending_middleware_solved");
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_previously_failed_escalated_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, Ticket ticket)
        {
            ticket.Tags.Add("sending_middleware_escalated");
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().EscalateTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Marks_ticket_as_shared([Frozen] FakeZendeskApi zendesk, Watcher sut, [Pending(As.Solved)] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware_solved")
                .And.NotContain("sending_middleware_solved");
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

        public class MemberAutoDomainDataAttribute : MemberAutoDataAttribute
        {
            public MemberAutoDomainDataAttribute(string memberName, params object[] parameters)
                : base(new AutoDataDomainAttribute(), memberName, parameters)
            {
            }
        }
    }
}

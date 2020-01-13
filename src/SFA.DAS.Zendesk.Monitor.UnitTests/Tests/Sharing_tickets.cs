using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixtureCustomisation;
using SFA.DAS.Zendesk.Monitor.UnitTests.Helpers;
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
            yield return new object[] { "escalated" };
            yield return new object[] { "solved" };
        }

        [Theory, MemberAutoDomainData(nameof(Tags))]
        public async Task Marks_ticket_as_sharing_before_sending_to_middleware(string state, [Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Escalated] Ticket ticket)
        {
            ticket.Tags = new List<string> { $"pending_middleware_{state}".ToLower() };
            zendesk.Tickets.Add(ticket);
            middleware.When(x => x.SolveTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(_ => throw new Exception("Stop test at Middleware step"));
            middleware.When(x => x.EscalateTicket(Arg.Any<Middleware.EventWrapper>()))
                .Do(_ => throw new Exception("Stop test at Middleware step"));

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
        public async Task Sends_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Solved] Ticket ticket)
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
        public async Task Sends_ticket_to_correct_endpoint([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Escalated] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().EscalateTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_nothing_to_middleware_when_ther_are_no_tagged_comments(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            [Frozen] AuditedComment comment,
            [Pending.Solved] Ticket ticket,
            Watcher sut
                                                                                      )
        {
            // Given
            comment.AuditTagEvent.Value = "";
            zendesk.Tickets.Add(ticket);

            // When
            await sut.ShareTicket(ticket.Id);

            // Then
            await middleware.DidNotReceive().SolveTicket(Arg.Any<Middleware.EventWrapper>());
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_tagged_comment(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Frozen] AuditedComment comment,
            [Pending.Solved] Ticket ticket)
        {
            // Given
            var auditTagEvent = comment.AuditTagEvent.Value = "escalated_tag";
            zendesk.Tickets.Add(ticket);

            // When
            await sut.ShareTicket(ticket.Id);

            // Then
            var mwt = new { Ticket = new { Comments = new[] { new { comment.Id } } } };

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x => x.Should().BeEquivalentTo(mwt)));
        }

        //[Theory, AutoDataDomain]
        //public async Task Sends_ticket_to_middleware_when_tagged_comment_has_other_tags_too(
        //    [Frozen] FakeZendeskApi zendesk,
        //    [Frozen] Middleware.IApi middleware,
        //    Watcher sut,
        //    [Frozen] AuditedComment comment,
        //    [Pending.Solved] Ticket ticket)
        //{
        //    // Given
        //    var auditTagEvent = comment.AuditTagEvent.Value = "something, escalated_tag, another";
        //    zendesk.Tickets.Add(ticket);
        //}

        [Theory, AutoData]
        public void AuditedCommentMapping(AuditedComment comment)
        {
            comment.Id.Should().Be(comment.AsAudit.Events[^1].Id);
            comment.Public.Should().Be(comment.AsAudit.Events[^1].Public);
            comment.Body.Should().Be(comment.AsAudit.Events[^1].Body);
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_all_tagged_comments(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved(addComment: false)] Ticket ticket,
            AuditedComment[] comments)
        {
            // Given
            comments.ShareAll();
            zendesk.Tickets.Add(ticket);
            zendesk.AddComments(ticket, comments);

            // When
            await sut.ShareTicket(ticket.Id);

            // Then
            var mwt = new
            {
                Ticket = new
                {
                    Comments = comments.Select(x =>
                    new
                    {
                        x.Id,
                        x.Body,
                    }),
                }
            };

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(
                    body => body.Should().BeEquivalentTo(mwt)));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_attachments(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.Solved(addComment: false)] Ticket ticket,
            AuditedComment[] comments)
        {
            // Given
            comments.ShareAll();
            zendesk.Tickets.Add(ticket);
            zendesk.AddComments(ticket, comments);

            // When
            await sut.ShareTicket(ticket.Id);

            // Then
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

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(
                    body => body.Should().BeEquivalentTo(expected)));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_ticket_to_middleware_with_requester([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Solved] Ticket ticket, User reporter)
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
        public async Task Sends_ticket_to_middleware_with_organisation([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Solved] Ticket ticket, Organization org)
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
                            MainPhone = org.OrganizationFields.MainPhone.ToString(),
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

        [Theory, AutoDataDomain]
        public async Task Sends_previously_failed_solved_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Solved] Ticket ticket)
        {
            ticket.Tags = new List<string> { "sending_middleware_solved" };
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().SolveTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Sends_previously_failed_escalated_ticket_to_middleware([Frozen] FakeZendeskApi zendesk, [Frozen] Middleware.IApi middleware, Watcher sut, [Pending.Escalated] Ticket ticket)
        {
            ticket.Tags = new List<string> { "sending_middleware_escalated" };
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            await middleware.Received().EscalateTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(new { Ticket = new { ticket.Id } })));
        }

        [Theory, AutoDataDomain]
        public async Task Marks_ticket_as_shared([Frozen] FakeZendeskApi zendesk, Watcher sut, [Pending.Solved] Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.ShareTicket(ticket.Id);

            zendesk.Tickets.First(x => x.Id == ticket.Id)
                .Tags
                .Should().NotContain("pending_middleware_solved")
                .And.NotContain("sending_middleware_solved");
        }

        [Theory, AutoDataDomain]
        public async Task Marks_ticket_as_shared_with_duplicate_tags([Frozen] FakeZendeskApi zendesk, Watcher sut, [Pending.Solved] Ticket ticket)
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
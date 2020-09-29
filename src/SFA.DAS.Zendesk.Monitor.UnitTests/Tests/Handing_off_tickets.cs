using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixtureCustomisation;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class Handing_off_tickets
    {
        [Theory, ZendeskAutoData]
        public async Task Sends_ticket_to_middleware(
            [Frozen] FakeZendeskApi zendesk,
            [Frozen] Middleware.IApi middleware,
            Watcher sut,
            [Pending.HandedOff] Ticket ticket)
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

            await middleware.Received().HandOffTicket(
                Verify.That<Middleware.EventWrapper>(x =>
                    x.Should().BeEquivalentTo(expectedTicket)));
        }
    }
}
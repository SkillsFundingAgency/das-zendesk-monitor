using FluentAssertions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using ZenWatch.Acceptance.Fakes;
using ZenWatch.Zendesk;

namespace ZenWatch.Acceptance
{
    public class Data
    {
        public Ticket Ticket { get; set; }
    }

    [Binding]
    public class MiddlewareReceiveTicketUpdatesSteps
    {
        private readonly MockMiddleware middleware = new MockMiddleware();

        private readonly Watcher watcher;
        private readonly FakeZendesk zendesk = new FakeZendesk();
        private readonly Data data;

        public MiddlewareReceiveTicketUpdatesSteps(Data data)
        {
            this.data = data;
            watcher = new Watcher(zendesk, middleware);
        }

        [Given(@"a ticket exists")]
        public async Task GivenATicketExists()
        {
            data.Ticket = await zendesk.CreateTicket();
        }

        [When(@"the ticket is marked to be shared")]
        public async Task WhenTheTicketIsMarkedToBeShared()
        {
            data.Ticket.Tags.Add("pending_middleware");
            await zendesk.UpdateTicket(data.Ticket);

            // Zendesk search results are only updated every ??? minutes.
            await WaitUntil(() => TicketIsMarkedForSharing(data.Ticket.Id), TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(20));

            // Doesn't really belong in "When", but has to happen before the "Thens"
            await watcher.Watch();
        }

        private async Task<bool> TicketIsMarkedForSharing(long id)
        {
            var ticket = await zendesk.GetTicketsForSharing();
            return ticket.Any(x => x == id);
        }

        private async Task<bool> WaitUntil(Func<Task<bool>> p, TimeSpan timeSpan, TimeSpan waitBetween)
        {
            var now = DateTime.UtcNow;
            var until = now.Add(timeSpan);
            while (DateTime.UtcNow < until)
            {
                var success = await p();
                Debug.WriteLine($"WaitUntil {DateTime.UtcNow - now} successful? {success}");
                if (success) return success;
                await Task.Delay(waitBetween);
            }
            return false;
        }

        [Then(@"the ticket is shared with the Middleware")]
        public async Task ThenTheTicketIsSharedWithTheMiddleware()
        {
            var received = await middleware.TicketEvents();
            received.Should().ContainEquivalentOf(new { data.Ticket.Id });
        }

        [Then(@"the ticket is not marked to be shared")]
        public async Task ThenTheTicketIsNotMarkedToBeShared()
        {
            var ticket = await zendesk.GetTicket(data.Ticket.Id);
            ticket.Tags.Should().NotContain("pending_middleware");
        }
    }
}
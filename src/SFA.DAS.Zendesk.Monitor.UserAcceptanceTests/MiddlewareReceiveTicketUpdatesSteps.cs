using FluentAssertions;
using Reqnroll;
using Reqnroll.Tracing;
using SFA.DAS.Zendesk.Monitor.Acceptance.Fakes;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Acceptance
{
    public class Data
    {
        public Ticket Ticket { get; set; }
    }

    [Binding]
    public class MiddlewareReceiveTicketUpdatesSteps
    {
        private const string PendingMiddleware = "pending_middleware";
        private readonly MockMiddleware middleware = new MockMiddleware();

        private readonly Watcher watcher;
        private readonly FakeZendesk zendesk = new FakeZendesk();
        private readonly Data data;
        private readonly ITraceListener trace;

        public MiddlewareReceiveTicketUpdatesSteps(Data data, ITraceListener traceListener)
        {
            this.data = data;
            this.trace = traceListener;
            watcher = new Watcher(zendesk, middleware);
        }

        [Given(@"a ticket exists")]
        public async Task GivenATicketExists()
        {
            data.Ticket = await zendesk.CreateTicket();
            trace.WriteTestOutput($"Ticket {data.Ticket.Id} created - {data.Ticket.Url}");
        }

        [When(@"the ticket is marked to be shared")]
        public async Task WhenTheTicketIsMarkedToBeShared()
        {
            await zendesk.AddTag(data.Ticket, PendingMiddleware);

            // Zendesk search results are only updated every ??? minutes.
            await WaitUntil(() => TicketIsMarkedForSharing(data.Ticket.Id), TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(20));

            // Doesn't really belong in "When", but has to happen before the "Thens"
        }

        private async Task TicketIsMarkedForSharing(long id)
        {
            var ticket = await watcher.GetTicketsForSharing();
            ticket.Should().Contain(id);
        }

        private async Task WaitUntil(Func<Task> p, TimeSpan timeSpan, TimeSpan waitBetween)
        {
            var now = DateTime.UtcNow;
            var until = now.Add(timeSpan);
            while (true)
            {
                try
                {
                    await p();
                    return;
                }
                catch
                {
                    if (DateTime.UtcNow > until)
                        throw;
                }
                await Task.Delay(waitBetween);
            }
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
            ticket.Tags.Should().NotContain(PendingMiddleware);
        }

        [When(@"the ticket is marked for escalation to Service Now")]
        public async Task WhenHasBeenMarkedForEscalationToServiceNow()
        {
            await zendesk.Escalate(data.Ticket);
        }

        [Then(@"the ticket is updated with the Incident Number")]
        public async Task ThenTheTicketIsUpdatedWithTheIncidentNumberAsync()
        {
            var incidentNumberFieldId = await zendesk.CustomTicketFieldId("Service Now Incident Number (auto populated)");

            await WaitUntil(
                TicketHasIncidentNumber,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(10));

            async Task TicketHasIncidentNumber()
            {
                var ticket = await zendesk.GetTicket(data.Ticket.Id);
                var incNo = ticket.CustomField(incidentNumberFieldId)?.Value;
                incNo.Should().NotBeNull()
                    .And.Subject.ToString().Should().NotBeEmpty();
                trace.WriteTestOutput($"Incident Number: `{incNo}`");
            }
        }

        [AfterScenario]
        public async Task SolveTestTicket()
        {
            if ((data?.Ticket?.Id > 0) == false) return;

            await zendesk.Solve(data.Ticket);
            trace.WriteTestOutput($"Ticket {data.Ticket.Id} solved");
        }
    }
}
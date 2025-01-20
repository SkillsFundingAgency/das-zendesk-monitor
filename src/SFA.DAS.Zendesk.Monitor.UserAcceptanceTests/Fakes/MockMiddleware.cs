using Newtonsoft.Json;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WireMock.Admin;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.Zendesk.Monitor.Acceptance
{
    internal class MockMiddleware : Middleware.IApi
    {
        private readonly WireMockServer server;
        private readonly Middleware.IApi client;

        public MockMiddleware()
        {
            server = WireMockServer.Start(new WireMockServerSettings
            {
                StartAdminInterface = true,
            });

            server
                .Given(Request.Create().WithPath("/event").UsingAnyMethod())
                .RespondWith(Response.Create().WithSuccess());

            client = RestClient.For<Middleware.IApi>(server.Urls[0]);
        }

        public string SubscriptionKey { get; set; }

        public Task HandOffTicket([Body] EventWrapper body)
            => client.HandOffTicket(body);

        public Task EscalateTicket([Body] EventWrapper body)
            => client.EscalateTicket(body);

        public Task SolveTicket([Body] Middleware.EventWrapper body)
            => client.SolveTicket(body);

        public async Task<IReadOnlyList<Zendesk.Model.Ticket>> TicketEvents()
        {
            var logEntries = server.LogEntries
                .Where(entry => entry.RequestMessage.Url.EndsWith("/event", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tickets = logEntries
                .Select(x => JsonConvert.DeserializeObject<Zendesk.Model.TicketResponse>(x.RequestMessage.Body))
                .Where(ticketResponse => ticketResponse != null)
                .Select(ticketResponse => ticketResponse.Ticket)
                .ToList();

            return tickets;
        }
    }
}
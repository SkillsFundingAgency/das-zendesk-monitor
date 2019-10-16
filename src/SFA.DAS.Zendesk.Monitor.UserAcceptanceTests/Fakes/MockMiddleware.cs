using Newtonsoft.Json;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Middleware;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WireMock.Client;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.Zendesk.Monitor.Acceptance
{
    internal class MockMiddleware : Middleware.IApi
    {
        private readonly FluentMockServer server;
        private readonly Middleware.IApi client;
        private readonly IFluentMockServerAdmin admin;

        public MockMiddleware()
        {
            server = FluentMockServer.Start(new FluentMockServerSettings
            {
                StartAdminInterface = true,
            });

            server
                .Given(Request.Create().WithPath("/event").UsingAnyMethod())
                .RespondWith(Response.Create().WithSuccess());

            client = RestClient.For<Middleware.IApi>(server.Urls[0]);

            admin = RestClient.For<IFluentMockServerAdmin>(server.Urls[0]);
        }

        public string SubscriptionKey { get; set; }

        public Task PostEvent([Body] Middleware.EventWrapper body)
            => client.PostEvent(body);

        public Task PostEvent([Body] EW2 body)
            => client.PostEvent(body);

        public async Task<IReadOnlyList<Zendesk.Model.Ticket>> TicketEvents()
        {
            var entries = await admin.GetRequestsAsync();
            return entries
                .Where(x => x.Request.Url.EndsWith("/event"))
                .Select(x => JsonConvert.DeserializeObject<Zendesk.Model.TicketResponse>(x.Request.Body).Ticket)
                .ToList();
        }
    }
}
using Newtonsoft.Json;
using Refit;
using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.Client;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace ZenWatch.Acceptance
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

            client = RestService.For<Middleware.IApi>(server.Urls[0]);

            admin = RestClient.For<IFluentMockServerAdmin>(server.Urls[0]);
        }

        public async Task PostEvent([Refit.Body] Middleware.EventWrapper body)
            => await client.PostEvent(body);

        public async Task<IReadOnlyList<Zendesk.Ticket>> TicketEvents()
        {
            var entries = await admin.GetRequestsAsync();
            return entries
                .Where(x => x.Request.Url.EndsWith("/event"))
                .Select(x => JsonConvert.DeserializeObject<Zendesk.Empty>(x.Request.Body).Ticket)
                .ToList();
        }
    }
}
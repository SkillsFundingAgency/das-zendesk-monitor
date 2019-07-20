using NSubstitute;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace ZenWatch.Acceptance
{
    public class Ticket
    {
        public string File { get; internal set; }

        internal void MarkForSharing()
        {
            File = "TicketWithSharing";
        }
    }

    public class Zendesk
    {
        internal Ticket CreateTicket()
        {
            return new Ticket { File = "TicketWithouSharing" };
        }
    }
    public class Data
    {
        public Ticket Ticket { get; set; }
    }

    public abstract class MockHandler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SendAsync(request.Method, request.RequestUri.PathAndQuery));
        }

        public abstract HttpResponseMessage SendAsync(HttpMethod method, string url);
    }


    [Binding]
    public class MiddlewareReceiveTicketUpdatesSteps
    {
        private readonly CannedZendeskApi api = new CannedZendeskApi();
        private readonly MockMiddlewareApi middleware = new MockMiddlewareApi();
        private readonly Watcher watcher;
        private readonly Zendesk zendesk = new Zendesk();
        private readonly Data data;

        public MiddlewareReceiveTicketUpdatesSteps(Data data)
        {
            this.data = data;
            watcher = new Watcher(api, middleware);
        }

        [Given(@"a ticket exists")]
        public void GivenATicketExists()
        {
            data.Ticket = zendesk.CreateTicket();
        }
        
        [When(@"the ticket is marked to be shared")]
        public async Task WhenTheTicketIsMarkedToBeShared()
        {
            api.UseTicket(data.Ticket);
            data.Ticket.MarkForSharing();
            await watcher.Watch2();
        }
        
        [Then(@"the ticket is shared with the Middleware")]
        public void ThenTheTicketIsSharedWithTheMiddleware()
        {
            middleware.Handler.Received().SendAsync(HttpMethod.Post, Arg.Is<string>(x => x.EndsWith("/event")));
        }
    }
}

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Fakes
{
    public class HttpMessageHandlerSpy : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; set; } = new List<HttpRequestMessage>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            Requests.Add(request);

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("my string, that needs to be returned")
            };
        }
    }
}
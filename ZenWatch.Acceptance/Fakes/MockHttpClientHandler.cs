using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZenWatch.Acceptance.Fakes
{
    public abstract class MockHttpClientHandler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SendAsync(request.Method, request.RequestUri.PathAndQuery, request.Content));
        }

        public abstract HttpResponseMessage SendAsync(HttpMethod method, string url, HttpContent content);
    }
}
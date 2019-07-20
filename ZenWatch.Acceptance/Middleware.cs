using NSubstitute;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZenWatch.Acceptance
{
    internal class MockMiddlewareApi : Middleware.IApi
    {
        private readonly HttpClient client;

        public MockMiddlewareApi()
        {
            Handler = Substitute.ForPartsOf<MockHandler>();
            Handler.SendAsync(Arg.Any<HttpMethod>(), Arg.Any<string>()).Returns(Success());

            client = new HttpClient(Handler)
            {
                BaseAddress = new Uri("https://posthere.io/5f15-46df-8bb8")
            };
        }

        public MockHandler Handler { get; }

        private static HttpResponseMessage Success(string content = null)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            if (content != null) response.Content = new StringContent(content);
            return response;
        }

        public async Task PostEvent([Body] Middleware.EventWrapper body)
        {
            await RestService.For<Middleware.IApi>(client).PostEvent(body);
        }
    }
}
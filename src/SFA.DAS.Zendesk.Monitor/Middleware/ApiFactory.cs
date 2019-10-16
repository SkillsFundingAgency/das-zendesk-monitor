using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class ApiFactory
    {
        private readonly string subscriptionKey;
        private readonly ILogger<LoggingHttpClientHandler> logger;
        private readonly HttpClient httpClient;

        public ApiFactory(Uri url, string subscriptionKey, string basicAuth, ILogger<LoggingHttpClientHandler> logger)
        {
            this.subscriptionKey = subscriptionKey;
            this.logger = logger;

            httpClient = new HttpClient(new LoggingHttpClientHandler(logger))
            {
                BaseAddress = url
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        }

        public IApi Create()
        {
            var api = new RestClient(httpClient).CreateApi();
            api.SubscriptionKey = subscriptionKey;
            return api;
        }
    }

    public static class ApiFactoryExtensions
    {
        private static readonly JsonSerializerSettings serialiser = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static IApi CreateApi(this RestClient client)
        {
            client.JsonSerializerSettings = serialiser;
            return client.For<IApi>();
        }
    }
}
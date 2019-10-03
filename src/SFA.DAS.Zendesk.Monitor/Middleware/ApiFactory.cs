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
        private readonly ILogger<LoggingHttpClientHandler> logger;
        private readonly HttpClient httpClient;

        public ApiFactory(Uri url, string basicAuth, ILogger<LoggingHttpClientHandler> logger)
        {
            this.logger = logger;

            httpClient = new HttpClient(new LoggingHttpClientHandler(logger))
            {
                BaseAddress = url
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        }

        public IApi Create() => new RestClient(httpClient).CreateApi();
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
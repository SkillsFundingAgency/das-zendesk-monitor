using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "<Pending>")]
    public class ApiFactory
    {
        private readonly string subscriptionKey;
        private readonly ILogger<LoggingHttpClientHandler> logger;
        private readonly HttpClient httpClient;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
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
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() },
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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public static class ApiFactory
    {
        public static IApi CreateApi(HttpClient client, Uri url, string subscriptionKey, string basicAuth)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (url == null) throw new ArgumentNullException(nameof(url));

            client.BaseAddress = url;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var api = new RestClient(client).CreateApi();
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
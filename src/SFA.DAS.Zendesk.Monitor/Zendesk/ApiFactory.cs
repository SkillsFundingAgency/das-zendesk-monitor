using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class ApiFactory
    {
        private readonly HttpClient httpClient;

        public ApiFactory(Uri url, string user, string password, ILogger<LoggingHttpClientHandler> logger)
        {
            if (!url.AbsolutePath.Contains("api/v2"))
                url = new Uri(url, "api/v2");

            var authentication = Encoding.ASCII.GetBytes($"{user}:{password}");

            httpClient = new HttpClient(new LoggingHttpClientHandler(logger));
            httpClient.BaseAddress = url;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authentication));
        }

        public IApi CreateApi() => new RestClient(httpClient).CreateApi();

        public static IApi CreateApi(HttpClient client) => new RestClient(client).CreateApi();
    }

    public static class ApiFactoryExtensions
    {
        public static readonly JsonSerializerSettings serialiser = new JsonSerializerSettings
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
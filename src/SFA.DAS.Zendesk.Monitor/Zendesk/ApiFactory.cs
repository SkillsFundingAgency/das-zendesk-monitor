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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "<Pending>")]
    public class ApiFactory
    {
        private readonly HttpClient httpClient;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
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

    internal static class ApiFactoryExtensions
    {
        public static readonly JsonSerializerSettings serialiser = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        internal static IApi CreateApi(this RestClient client)
        {
            client.JsonSerializerSettings = serialiser;
            return client.For<IApi>();
        }
    }
}
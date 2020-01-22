using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class ApiFactory
    {
        public static IApi CreateApi(HttpClient httpClient, Uri url, string user, string password)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (url == null) throw new ArgumentNullException(nameof(url));

            if (!url.AbsolutePath.Contains("api/v2"))
                url = new Uri(url, "api/v2");

            var authentication = Encoding.ASCII.GetBytes($"{user}:{password}");

            httpClient.BaseAddress = url;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authentication));

            return new RestClient(httpClient).CreateApi();
        }

        public static IApi CreateApi(HttpClient client) => new RestClient(client).CreateApi();
    }

    public static class ApiFactoryExtensions
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
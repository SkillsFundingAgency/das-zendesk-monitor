using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ZenWatch.Zendesk
{
    public class ApiFactoryFactory
    {
        private readonly HttpClient httpClient;

        public ApiFactoryFactory(string instanceName, string user, string password)
        {
            var authentication = Encoding.ASCII.GetBytes($"{user}:{password}");

            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"https://{instanceName}.zendesk.com/api/v2/");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authentication));
        }

        public IApi CreateApi() => ApiFactory.Create(httpClient);
    }

    public static class ApiFactory
    {
        private static JsonSerializerSettings serialiser = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static IApi Create(HttpClient client)
        {
            return new RestClient(client).CreateApi();
        }

        public static IApi Create(string instanceName, string user, string password)
        {
            return new RestClient($"https://{instanceName}.zendesk.com/api/v2/").CreateApi();
        }

        private static IApi CreateApi(this RestClient client)
        {
            client.JsonSerializerSettings = serialiser;
            return client.For<IApi>();
        }
    }
}
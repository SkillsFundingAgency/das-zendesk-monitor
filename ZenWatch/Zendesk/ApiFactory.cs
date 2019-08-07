using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System.Net.Http;

namespace ZenWatch.Zendesk
{
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

        public static IApi Create(string instanceName)
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
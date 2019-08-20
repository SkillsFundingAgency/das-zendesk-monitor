using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class ApiFactoryFactory
    {
        private readonly HttpClient httpClient;

        public ApiFactoryFactory(string instanceName, string user, string password)
            : this(new Uri($"https://{instanceName}.zendesk.com/api/v2/"), user, password)
        {
        }

        public ApiFactoryFactory(Uri url, string user, string password)
        {
            if (!url.AbsolutePath.Contains("api/v2"))
                url = new Uri(url, "api/v2");

            var authentication = Encoding.ASCII.GetBytes($"{user}:{password}");

            httpClient = new HttpClient();
            httpClient.BaseAddress = url;
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
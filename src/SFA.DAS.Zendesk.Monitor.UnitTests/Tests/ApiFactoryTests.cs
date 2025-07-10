using System;
using System.Net.Http;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Middleware;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class ApiFactoryTests
    {
        [Fact]
        public void CreateApi_ThrowsArgumentNullException_WhenClientIsNull()
        {
            HttpClient client = null;
            var url = new Uri("https://example.com");
            var subscriptionKey = "key";
            var basicAuth = "auth";

            Assert.Throws<ArgumentNullException>(() =>
                ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth));
        }

        [Fact]
        public void CreateApi_ThrowsArgumentNullException_WhenUrlIsNull()
        {
            var client = new HttpClient();
            Uri url = null;
            var subscriptionKey = "key";
            var basicAuth = "auth";

            Assert.Throws<ArgumentNullException>(() =>
                ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth));
        }

        [Fact]
        public void CreateApi_SetsBaseAddressAndAuthorizationHeader_AndReturnsApiWithSubscriptionKey()
        {
            var client = new HttpClient();
            var url = new Uri("https://example.com");
            var subscriptionKey = "key";
            var basicAuth = "auth";

            var api = ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth);

            Assert.Equal(url, client.BaseAddress);
            Assert.Equal("Basic", client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal(basicAuth, client.DefaultRequestHeaders.Authorization.Parameter);
            Assert.Equal(subscriptionKey, api.SubscriptionKey);
        }


        [Fact]
        public void CreateApiExtension_ThrowsArgumentNullException_WhenRestClientIsNull()
        {
            RestClient client = null;

            var ex = Assert.Throws<ArgumentNullException>(() =>
                client.CreateApi());
            Assert.Contains("RestClient cannot be null", ex.Message);
        }

        [Fact]
        public void CreateApiExtension_SetsJsonSerializerSettings_AndReturnsApi()
        {
            var httpClient = new HttpClient();
            var restClient = new RestClient(httpClient);

            var api = restClient.CreateApi();

            Assert.NotNull(api);
            Assert.NotNull(restClient.JsonSerializerSettings);
            Assert.Equal(Newtonsoft.Json.NullValueHandling.Ignore, restClient.JsonSerializerSettings.NullValueHandling);
        }
    }
}

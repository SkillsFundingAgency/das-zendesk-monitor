using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Moq;
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
            // Arrange
            HttpClient client = null;
            var url = new Uri("https://example.com");
            var subscriptionKey = "key";
            var basicAuth = "auth";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth));
        }

        [Fact]
        public void CreateApi_ThrowsArgumentNullException_WhenUrlIsNull()
        {
            // Arrange
            var client = new HttpClient();
            Uri url = null;
            var subscriptionKey = "key";
            var basicAuth = "auth";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth));
        }

        [Fact]
        public void CreateApi_SetsBaseAddressAndAuthorizationHeader_AndReturnsApiWithSubscriptionKey()
        {
            // Arrange
            var client = new HttpClient();
            var url = new Uri("https://example.com");
            var subscriptionKey = "key";
            var basicAuth = "auth";

            // Act
            var api = ApiFactory.CreateApi(client, url, subscriptionKey, basicAuth);

            // Assert
            Assert.Equal(url, client.BaseAddress);
            Assert.Equal("Basic", client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal(basicAuth, client.DefaultRequestHeaders.Authorization.Parameter);
            Assert.Equal(subscriptionKey, api.SubscriptionKey);
        }


        [Fact]
        public void CreateApiExtension_ThrowsArgumentNullException_WhenRestClientIsNull()
        {
            // Arrange
            RestClient client = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                ApiFactoryExtensions.CreateApi(client));
            Assert.Contains("RestClient cannot be null", ex.Message);
        }

        [Fact]
        public void CreateApiExtension_SetsJsonSerializerSettings_AndReturnsApi()
        {
            // Arrange
            var httpClient = new HttpClient();
            var restClient = new RestClient(httpClient);

            // Act
            var api = restClient.CreateApi();

            // Assert
            Assert.NotNull(api);
            Assert.NotNull(restClient.JsonSerializerSettings);
            // Optionally, check for expected settings (e.g., NullValueHandling)
            Assert.Equal(Newtonsoft.Json.NullValueHandling.Ignore, restClient.JsonSerializerSettings.NullValueHandling);
        }
    }
}

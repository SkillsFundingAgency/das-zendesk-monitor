using RestEase;
using System.Net.Http;

namespace ZenWatch.Middleware
{
    public class ApiFactory
    {
        public static IApi Create(string instanceName)
        {
            var client = new RestClient($"https://posthere.io/{instanceName}");
            return client.For<IApi>();
        }
    }
}
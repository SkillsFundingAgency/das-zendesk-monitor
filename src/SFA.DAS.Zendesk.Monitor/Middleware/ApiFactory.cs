using RestEase;
using System;
using System.Net.Http;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public class ApiFactory
    {
        public static IApi Create(string instanceName) => Create($"https://posthere.io/{instanceName}");

        public static IApi Create(Uri uri) => new RestClient(uri).For<IApi>();
    }
}
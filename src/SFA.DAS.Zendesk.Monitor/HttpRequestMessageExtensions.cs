using System;
using System.Net.Http;


namespace SFA.DAS.Zendesk.Monitor
{
    public static class HttpRequestMessageExtensions
    {
        public static string[] GetQuery(this HttpRequestMessage request, string queryName)
        {
            var qs = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var values = qs?[queryName]?.Split(",", StringSplitOptions.RemoveEmptyEntries);
            return values ?? new string[0];
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public static class DurableExtensions
    {
        public static HttpResponseMessage CreateCheckStatusResponse(this DurableOrchestrationClient starter, HttpRequest request, string instanceId)
        {
            var httpRequestMessage = new HttpRequestMessageFeature(request.HttpContext).HttpRequestMessage;
            return starter.CreateCheckStatusResponse(httpRequestMessage, instanceId);
        }
    }
}
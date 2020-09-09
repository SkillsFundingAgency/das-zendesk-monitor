using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;

namespace ZenWatchFunction
{
    public static class OrchestrationExtensions
    {
        public static bool OrchestrationIsRunning(this DurableOrchestrationStatus status)
            => status != null && OrchestrationIsRunning(status.RuntimeStatus);

        public static bool OrchestrationIsRunning(this OrchestrationRuntimeStatus status) =>
            status == OrchestrationRuntimeStatus.Running || status == OrchestrationRuntimeStatus.Pending;

        public static HttpResponseMessage CreateCheckStatusResponse(this IDurableOrchestrationClient starter, HttpRequest request, string instanceId)
        {
            var httpRequestMessage = new HttpRequestMessageFeature(request.HttpContext).HttpRequestMessage;
            return starter.CreateCheckStatusResponse(httpRequestMessage, instanceId);
        }
    }
}
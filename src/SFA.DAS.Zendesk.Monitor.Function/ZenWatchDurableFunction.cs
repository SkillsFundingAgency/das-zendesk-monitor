using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class WatcherOrchestration
    {
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";
        private static readonly RetryOptions retry = new RetryOptions(TimeSpan.FromSeconds(1), 5);

        private class NotifyTicket
        {
            public long Id { get; set; }
        }

        [FunctionName("NotifyTicket")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequest request,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            var ids = request.Query["id"].Select(x => long.Parse(x)).ToList();

            var content = await new StreamReader(request.Body).ReadToEndAsync();
            var ticket = JsonConvert.DeserializeObject<NotifyTicket>(content);
            if(ticket?.Id != null) ids.Add(ticket.Id);

            log.LogInformation("NotifyTicket {ids}", ids);

            var instanceId = await starter.StartNewAsync(nameof(ShareListedTickets), ids);
            return starter.CreateCheckStatusResponse(request, instanceId);
        }

        [FunctionName("WatcherEntryPoint")]
        public static Task Run(
            [TimerTrigger("* */30 * * * *")] TimerInfo _,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            return GetSingleInstance(starter, log);
        }

        private static async Task<DurableOrchestrationStatus> GetSingleInstance(DurableOrchestrationClient starter, ILogger log)
        {
            var instance = await starter.GetStatusAsync(WatcherInstance);

            if (instance?.OrchestrationIsRunning() != true)
            {
                log.LogDebug("Starting Watcher orchestration");
                await starter.StartNewAsync(nameof(ShareAllTickets), WatcherInstance, null);
            }
            else
            {
                log.LogDebug("Watcher orchestration is already running");
            }

            return instance;
        }

        [FunctionName(nameof(ShareAllTickets))]
        public static async Task ShareAllTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), null);

            await context.CallActivityAsync(nameof(ShareListedTickets), tickets);
        }

        [FunctionName(nameof(ShareListedTickets))]
        public static async Task ShareListedTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = context.GetInput<long[]>();

            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(DurableWatcher.ShareTicket), retry, ticket);
        }
    }
}
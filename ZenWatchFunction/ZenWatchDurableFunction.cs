using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class WatcherOrchestration
    {
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";
        private static readonly RetryOptions retry = new RetryOptions(TimeSpan.FromSeconds(1), 5);
        private readonly WatcherOnTheWalls watcher;

        public WatcherOrchestration(WatcherOnTheWalls watcher)
        {
            this.watcher = watcher;
        }

        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            var instance = await GetSingleInstance(starter, log);
            return starter.CreateCheckStatusResponse(req, instance.InstanceId);
        }

        [FunctionName("WatcherEntryPoint")]
        public Task Run(
            [TimerTrigger("*/30 * * * * *", RunOnStartup = true)] TimerInfo _,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            return GetSingleInstance(starter, log);
        }

        private async Task<DurableOrchestrationStatus> GetSingleInstance(DurableOrchestrationClient starter, ILogger log)
        {
            var instance = await starter.GetStatusAsync(WatcherInstance);

            if (instance?.OrchestrationIsRunning() != true)
            {
                log.LogDebug("Starting Watcher orchestration");
                await starter.StartNewAsync(nameof(ShareTickets), WatcherInstance, null);
            }
            else
            {
                log.LogDebug("Watcher orchestration is already running");
            }

            return instance;
        }

        [FunctionName(nameof(ShareTickets))]
        public async Task ShareTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(SearchTickets), null);

            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(SendTicketEvent), retry, ticket);
        }

        [FunctionName(nameof(SearchTickets))]
        public Task<long[]> SearchTickets([ActivityTrigger] DurableActivityContext _)
        {
            return watcher.SearchForTickets();
        }

        [FunctionName(nameof(SendTicketEvent))]
        public Task SendTicketEvent([ActivityTrigger] long id)
        {
            return watcher.ShareTicket(id);
        }
    }
}
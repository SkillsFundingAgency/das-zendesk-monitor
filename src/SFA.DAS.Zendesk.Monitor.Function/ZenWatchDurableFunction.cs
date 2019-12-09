using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
     public class WatcherOrchestration
    {
        private static readonly RetryOptions retry = new RetryOptions(TimeSpan.FromSeconds(1), 5);

        [FunctionName(nameof(ShareAllTickets))]
        public static async Task ShareAllTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), null);

            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(DurableWatcher.ShareTicket), retry, ticket);
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
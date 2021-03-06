using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    public class WatcherOrchestration
    {
        private static readonly RetryOptions retry = new RetryOptions(TimeSpan.FromSeconds(1), 5);

        [FunctionName(nameof(ShareAllTickets))]
        public static async Task ShareAllTickets([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), null);
            await ShareTickets(context, tickets);
        }

        [FunctionName(nameof(ShareListedTickets))]
        public static async Task ShareListedTickets([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var tickets = context.GetInput<long[]>();
            await ShareTickets(context, tickets);
        }

        private static async Task ShareTickets(IDurableOrchestrationContext context, long[] tickets)
        {
            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(DurableWatcher.ShareTicket), retry, ticket);
        }
    }
}
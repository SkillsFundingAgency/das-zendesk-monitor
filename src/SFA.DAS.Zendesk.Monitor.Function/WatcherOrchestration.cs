using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using System;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    public static class WatcherOrchestration
    {
        private static readonly TaskOptions retryOptions = TaskOptions.FromRetryPolicy(
            new RetryPolicy(
                firstRetryInterval: TimeSpan.FromSeconds(1),
                maxNumberOfAttempts: 5));

        [Function(nameof(ShareAllTickets))]
        public static async Task ShareAllTickets([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), null);
            await ShareTickets(context, tickets);
        }

        [Function(nameof(ShareListedTickets))]
        public static async Task ShareListedTickets([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var tickets = context.GetInput<long[]>();
            await ShareTickets(context, tickets);
        }

        private static async Task ShareTickets(TaskOrchestrationContext context, long[] tickets)
        {
            foreach (var ticket in tickets)
                await context.CallActivityAsync(nameof(DurableWatcher.ShareTicket), ticket, retryOptions);
        }
    }
}
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

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
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), string.Empty);
            await ShareTickets(context,tickets);
        }

        [Function(nameof(ShareListedTickets))]
        public static async Task ShareListedTickets([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var tickets = context.GetInput<long[]>();
            if (tickets == null || tickets.Length == 0)
            {
                return;
            }
            await ShareTickets(context, tickets);
        }

        private static async Task ShareTickets(TaskOrchestrationContext context, long[] ticketIds)
        {
            const int batchSize = 10;
            var logger = context.CreateReplaySafeLogger(nameof(ShareTickets));

            for (int i = 0; i < ticketIds.Length; i += batchSize)
            {
                var batch = ticketIds.Skip(i).Take(batchSize).ToList();

                var tasks = batch.Select(ticket => context.CallActivityAsync(nameof(DurableWatcher.ShareTicket), ticket));

                try
                {
                    await Task.WhenAll(tasks);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing batch of tickets.");
                }
            }
        }
    }
}
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
            int maxConcurrentActivities = 5; 

            for (int i = 0; i < ticketIds.Length; i += batchSize)
            {
                var batch = ticketIds.Skip(i).Take(batchSize).ToList();

                var semaphore = new SemaphoreSlim(maxConcurrentActivities, maxConcurrentActivities);

                var tasks = batch.Select(async ticket =>
                {
                    try
                    {
                        await semaphore.WaitAsync(); 

                        logger.LogInformation($"Starting Activity for ticket {ticket}");
                        await context.CallActivityAsync(nameof(DurableWatcher.ShareTicket), ticket);
                        logger.LogInformation($"Completed Activity for ticket {ticket}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing ticket {ticket}"); 
                        throw;
                    }
                    finally
                    {
                        semaphore.Release(); 
                    }
                });

                await Task.WhenAll(tasks);
            }
        }
    }
}
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
            await ShareTickets(context, tickets);
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
            var logger = context.CreateReplaySafeLogger(nameof(ShareTickets));

            foreach (var ticketId in ticketIds)
            {
                try
                {
                    logger.LogInformation("Starting Activity for ticket {Ticket}", ticketId);
                    await context.CallActivityAsync(nameof(DurableWatcher.ShareTicket), ticketId, retryOptions);
                    logger.LogInformation("Completed Activity for ticket {Ticket}", ticketId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing ticket {TicketId}: {Message}", ticketId, ex.Message);
                }
            }
        }
    }
}
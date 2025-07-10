using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Zendesk.Monitor;

namespace ZenWatchFunction
{
    public class DurableWatcher
    {
        public DurableWatcher(Watcher watcher, ILogger<DurableWatcher> log)
        {
            this.watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        [Function(nameof(SearchTickets))]
        public async Task<long[]> SearchTickets([ActivityTrigger] string input)
        {
            var tickets = await watcher.GetTicketsForSharing();
            return tickets?.ToArray() ?? Array.Empty<long>();
        }

        [Function(nameof(ShareTicket))]
        public Task ShareTicket([ActivityTrigger] long id)
        {
            log.LogInformation("Sharing ticket {TicketId}", id);
            return watcher.ShareTicket(id);
        }

        private readonly ILogger<DurableWatcher> log;
        private readonly Watcher watcher;
    }
}
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Zendesk.Monitor;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<long[]> SearchTickets([ActivityTrigger] DurableTaskClient context)
        {
            log.LogInformation($"Searching for tickets");
            var tickets = await watcher.GetTicketsForSharing();
            log.LogInformation("Found {TicketCount}", tickets.Length);
            return tickets.ToArray();
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
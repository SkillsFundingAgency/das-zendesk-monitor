using Microsoft.Azure.WebJobs;
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

        [FunctionName(nameof(SearchTickets))]
        public async Task<long[]> SearchTickets([ActivityTrigger] DurableActivityContext context)
        {
            log.LogInformation($"Searching for tickets");
            var tickets = await watcher.GetTicketsForSharing();
            log.LogInformation($"Found {tickets.Count()}");
            return tickets.ToArray();
        }

        [FunctionName(nameof(ShareTicket))]
        public Task ShareTicket([ActivityTrigger] long id)
        {
            log.LogInformation($"Sharing ticket {id}");
            return watcher.ShareTicket(id);
        }

        private readonly ILogger<DurableWatcher> log;
        private readonly Watcher watcher;
    }
}
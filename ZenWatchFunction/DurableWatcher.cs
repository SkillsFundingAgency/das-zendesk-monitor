using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using ZenWatch;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class DurableWatcher
    {
        public DurableWatcher(Watcher watcher, ILogger<DurableWatcher> log)
        {
            this.watcher = watcher;
            this.log = log;
        }

        [FunctionName(nameof(SearchTickets))]
        public async Task<long[]> SearchTickets([ActivityTrigger] DurableActivityContext _)
        {
            log?.LogInformation($"Searching for tickets");
            var tickets = await watcher.GetTicketsForSharing();
            log?.LogInformation($"Found {tickets.Count()}");
            return tickets.ToArray();
        }

        [FunctionName(nameof(ShareTicket))]
        public Task ShareTicket([ActivityTrigger] long id)
        {
            log?.LogInformation($"Sharing ticket {id}");
            return watcher.ShareTicket(id);
        }

        private readonly ILogger<DurableWatcher> log;
        private readonly Watcher watcher;
    }
}
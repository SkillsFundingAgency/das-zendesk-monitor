using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    public static class RecoveryOrchestration
    {
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";

        [Function("BackgroundTaskEntryPoint")]
        public static Task Run(
            [TimerTrigger("%MonitorCronSetting%")] TimerInfo timer,
            [DurableClient] DurableTaskClient starter,
            ILogger log)
        {
            return GetSingleInstance(starter, log);
        }

        private static async Task<OrchestrationMetadata> GetSingleInstance(DurableTaskClient starter, ILogger log)
        {
            var instance = await starter.GetInstanceAsync(WatcherInstance);

            if (instance?.RuntimeStatus.OrchestrationIsRunning() != true)
            {
                log.LogInformation("Starting Watcher orchestration");
                await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WatcherOrchestration.ShareAllTickets), WatcherInstance);
            }
            else
            {
                log.LogWarning("Watcher orchestration is already running");
            }

            return instance;
        }
    }
}
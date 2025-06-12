using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using System.Diagnostics.CodeAnalysis;

namespace ZenWatchFunction
{
    [ExcludeFromCodeCoverage]
    public static class RecoveryOrchestration
    {
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";

        [Function("BackgroundTaskEntryPoint")]
        public static async Task Run(
            [TimerTrigger("%MonitorCronSetting%", RunOnStartup = false)] TimerInfo timer,
            [DurableClient] DurableTaskClient starter)
        {
            await GetSingleInstance(starter);
        }

        private static async Task GetSingleInstance(DurableTaskClient starter)
        {
            var instance = await starter.GetInstanceAsync(WatcherInstance);

            if (instance == null || instance.RuntimeStatus.OrchestrationIsRunning() != true)
            {
                await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WatcherOrchestration.ShareAllTickets), WatcherInstance);
            }
        }
    }
}
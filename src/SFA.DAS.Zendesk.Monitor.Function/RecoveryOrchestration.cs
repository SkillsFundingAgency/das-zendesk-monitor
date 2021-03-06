﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    public class RecoveryOrchestration
    {
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";

        [FunctionName("BackgroundTaskEntryPoint")]
        public static Task Run(
            [TimerTrigger("%MonitorCronSetting%")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            return GetSingleInstance(starter, log);
        }

        private static async Task<DurableOrchestrationStatus> GetSingleInstance(IDurableOrchestrationClient starter, ILogger log)
        {
            var instance = await starter.GetStatusAsync(WatcherInstance);

            if (instance?.OrchestrationIsRunning() != true)
            {
                log.LogInformation("Starting Watcher orchestration");
                await starter.StartNewAsync(nameof(WatcherOrchestration.ShareAllTickets), WatcherInstance);
            }
            else
            {
                log.LogWarning("Watcher orchestration is already running");
            }

            return instance;
        }
    }
}
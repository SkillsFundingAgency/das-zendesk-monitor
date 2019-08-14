using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public static class OrchestrationExtensions
    {
        public static bool OrchestrationIsRunning(this DurableOrchestrationStatus status)
            => status != null && OrchestrationIsRunning(status.RuntimeStatus);

        public static bool OrchestrationIsRunning(this OrchestrationRuntimeStatus status) =>
            status == OrchestrationRuntimeStatus.Running || status == OrchestrationRuntimeStatus.Pending;
    }
}
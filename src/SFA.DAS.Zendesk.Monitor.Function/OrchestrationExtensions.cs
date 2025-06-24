using Microsoft.DurableTask.Client;

namespace ZenWatchFunction
{
    public static class OrchestrationExtensions
    { 
        public static bool OrchestrationIsRunning(this OrchestrationRuntimeStatus status) =>
            status == OrchestrationRuntimeStatus.Running || status == OrchestrationRuntimeStatus.Pending;
    }
}
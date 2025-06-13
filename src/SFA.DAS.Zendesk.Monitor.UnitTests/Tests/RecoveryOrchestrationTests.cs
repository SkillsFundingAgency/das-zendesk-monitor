using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Xunit;
using ZenWatchFunction;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class RecoveryOrchestrationTests
    {
        [Fact]
        public async Task Run_DoesNotThrow_WhenCalled()
        {
            var timerInfo = CreateTimerInfo();
            var fakeClient = new FakeDurableTaskClient();

            var exception = await Record.ExceptionAsync(() => RecoveryOrchestration.Run(timerInfo, fakeClient));
            Assert.Null(exception);
        }

        private static TimerInfo CreateTimerInfo()
        {
            try
            {
                return new TimerInfo();
            }
            catch
            {
                return (TimerInfo)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TimerInfo));
            }
        }
    }

    public class FakeDurableTaskClient : DurableTaskClient
    {
        public FakeDurableTaskClient() : base("Fake") { }

        public override Task<string> ScheduleNewOrchestrationInstanceAsync(
            TaskName orchestratorName,
            object input = null,
            StartOrchestrationOptions options = null,
            CancellationToken cancellation = default)
            => Task.FromResult("fake-instance-id");

        public override Task RaiseEventAsync(
            string instanceId, string eventName, object eventPayload = null, CancellationToken cancellation = default)
            => Task.CompletedTask;

        public override Task<OrchestrationMetadata> WaitForInstanceStartAsync(
            string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellation = default)
            => Task.FromResult<OrchestrationMetadata>(null);

        public override Task<OrchestrationMetadata> WaitForInstanceCompletionAsync(
            string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellation = default)
            => Task.FromResult<OrchestrationMetadata>(null);

        public override Task SuspendInstanceAsync(
            string instanceId, string reason = null, CancellationToken cancellation = default)
            => Task.CompletedTask;

        public override Task ResumeInstanceAsync(
            string instanceId, string reason = null, CancellationToken cancellation = default)
            => Task.CompletedTask;

        public override Task<OrchestrationMetadata> GetInstancesAsync(
            string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellation = default)
            => Task.FromResult<OrchestrationMetadata>(null);

        public override AsyncPageable<OrchestrationMetadata> GetAllInstancesAsync(OrchestrationQuery filter = null)
            => new EmptyAsyncPageable<OrchestrationMetadata>();

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class EmptyAsyncPageable<T> : AsyncPageable<T>
    {
        public override IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
        {
            return GetEmptyPages();

            async IAsyncEnumerable<Page<T>> GetEmptyPages()
            {
                yield break;
            }
        }
    }
}

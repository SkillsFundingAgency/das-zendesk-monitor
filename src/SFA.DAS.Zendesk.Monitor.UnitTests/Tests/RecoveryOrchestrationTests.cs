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
        private const string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";

        [Fact]
        public async Task Run_DoesNotThrow_WhenCalled()
        {
            var timerInfo = CreateTimerInfo();
            var fakeClient = new FakeDurableTaskClient();

            var exception = await Record.ExceptionAsync(() => RecoveryOrchestration.Run(timerInfo, fakeClient));
            Assert.Null(exception);
        }

        [Fact]
        public async Task Starts_ShareAllTickets_with_the_fixed_instance_id()
        {
            var client = new FakeDurableTaskClient();

            await RecoveryOrchestration.Run(CreateTimerInfo(), client);

            Assert.Equal(1, client.ScheduleCount);
            Assert.Equal(WatcherInstance, client.LastOptions?.InstanceId);
            Assert.Null(client.LastInput);
        }

        [Theory]
        [InlineData(OrchestrationRuntimeStatus.Running)]
        [InlineData(OrchestrationRuntimeStatus.Pending)]
        public async Task Does_not_start_a_new_run_while_the_previous_one_is_running(OrchestrationRuntimeStatus status)
        {
            var client = new FakeDurableTaskClient { ExistingStatus = status };

            await RecoveryOrchestration.Run(CreateTimerInfo(), client);

            Assert.Equal(0, client.ScheduleCount);
        }

        [Theory]
        [InlineData(OrchestrationRuntimeStatus.Completed)]
        [InlineData(OrchestrationRuntimeStatus.Failed)]
        [InlineData(OrchestrationRuntimeStatus.Terminated)]
        public async Task Starts_a_new_run_when_the_previous_one_has_finished(OrchestrationRuntimeStatus status)
        {
            var client = new FakeDurableTaskClient { ExistingStatus = status };

            await RecoveryOrchestration.Run(CreateTimerInfo(), client);

            Assert.Equal(1, client.ScheduleCount);
            Assert.Equal(WatcherInstance, client.LastOptions?.InstanceId);
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
        public object? LastInput { get; private set; }
        public StartOrchestrationOptions? LastOptions { get; private set; }
        public int ScheduleCount { get; private set; }
        public OrchestrationRuntimeStatus? ExistingStatus { get; set; }   // null = no existing instance

        public FakeDurableTaskClient() : base("Fake") { }

        public override Task<string> ScheduleNewOrchestrationInstanceAsync(
            TaskName orchestratorName,
            object input = null,
            StartOrchestrationOptions options = null,
            CancellationToken cancellation = default)
        {
            ScheduleCount++;
            LastInput = input;
            LastOptions = options;
            return Task.FromResult(options?.InstanceId ?? "generated-id");
        }

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
            => Task.FromResult(ExistingStatus is null
                ? null
                : new OrchestrationMetadata(nameof(WatcherOrchestration.ShareAllTickets), instanceId)
                { RuntimeStatus = ExistingStatus.Value });

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

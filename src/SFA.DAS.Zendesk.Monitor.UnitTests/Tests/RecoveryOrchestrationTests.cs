//using System.Threading.Tasks;
//using Microsoft.DurableTask.Client;
//using Xunit;
//using ZenWatchFunction;

//namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
//{
//    public class RecoveryOrchestrationTests
//    {
//        [Fact]
//        public async Task Run_DoesNotThrow()
//        {
//            var fakeClient = new FakeDurableTaskClient();
//            var timerInfo = new Microsoft.Azure.Functions.Worker.TimerInfo();

//            await RecoveryOrchestration.Run(timerInfo, fakeClient);
//        }
//    }
//}
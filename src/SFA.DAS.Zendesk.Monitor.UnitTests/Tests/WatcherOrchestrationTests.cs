using System;
using System.Threading.Tasks;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZenWatchFunction;

public class WatcherOrchestrationTests
{
    [Fact]
    public async Task ShareAllTickets_CallsSearchTickets_And_SharesEachTicket()
    {
        var contextMock = new Mock<TaskOrchestrationContext>();
        var tickets = new long[] { 1, 2, 3, 4 };
        contextMock
            .Setup(c => c.CallActivityAsync<long[]>(
                new TaskName(nameof(DurableWatcher.SearchTickets)),
                It.IsAny<object>(),
                null))
            .ReturnsAsync(tickets);

        contextMock
            .Setup(c => c.CreateReplaySafeLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        contextMock
            .Setup(c => c.CallActivityAsync(
                new TaskName(nameof(DurableWatcher.ShareTicket)),
                It.IsAny<object>(),
                null))
            .Returns(Task.CompletedTask);

        await WatcherOrchestration.ShareAllTickets(contextMock.Object);

        contextMock.Verify(c => c.CallActivityAsync<long[]>(
            new TaskName(nameof(DurableWatcher.SearchTickets)),
            It.IsAny<object>(),
            null), Times.Once);
        contextMock.Verify(c => c.CallActivityAsync(
            new TaskName(nameof(DurableWatcher.ShareTicket)),
            It.IsAny<object>(),
            null), Times.Exactly(tickets.Length));
    }

    [Fact]
    public async Task ShareListedTickets_WithNullInput_DoesNothing()
    {
        var contextMock = new Mock<TaskOrchestrationContext>();
        contextMock.Setup(c => c.GetInput<long[]>()).Returns((long[])null);

        await WatcherOrchestration.ShareListedTickets(contextMock.Object);

        contextMock.Verify(c => c.GetInput<long[]>(), Times.Once);
    }

    [Fact]
    public async Task ShareListedTickets_WithEmptyInput_DoesNothing()
    {
        var contextMock = new Mock<TaskOrchestrationContext>();
        contextMock.Setup(c => c.GetInput<long[]>()).Returns(Array.Empty<long>());

        await WatcherOrchestration.ShareListedTickets(contextMock.Object);

        contextMock.Verify(c => c.GetInput<long[]>(), Times.Once);
    }

    [Fact]
    public async Task ShareListedTickets_WithTickets_SharesEachTicket()
    {
        var contextMock = new Mock<TaskOrchestrationContext>();
        var tickets = new long[] { 10, 20, 30 };
        contextMock.Setup(c => c.GetInput<long[]>()).Returns(tickets);

        contextMock
            .Setup(c => c.CreateReplaySafeLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        contextMock
            .Setup(c => c.CallActivityAsync(
                new TaskName(nameof(DurableWatcher.ShareTicket)),
                It.IsAny<object>(),
                null))
            .Returns(Task.CompletedTask);

        await WatcherOrchestration.ShareListedTickets(contextMock.Object);

        contextMock.Verify(c => c.CallActivityAsync(
            new TaskName(nameof(DurableWatcher.ShareTicket)),
            It.IsAny<object>(),
            null), Times.Exactly(tickets.Length));
    }
}

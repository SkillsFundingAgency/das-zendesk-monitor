using System;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Zendesk.Monitor;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using Xunit;
using ZenWatchFunction;

public class DurableWatcherTests
{
    private readonly Mock<ISharingTickets> _sharingTicketsMock;
    private readonly Mock<SFA.DAS.Zendesk.Monitor.Middleware.IApi> _apiMock;
    private readonly Mock<ILogger<Watcher>> _watcherLoggerMock;
    private readonly Mock<ILogger<DurableWatcher>> _loggerMock;
    private readonly Watcher _watcher;
    private readonly DurableWatcher _sut;

    public DurableWatcherTests()
    {
        _sharingTicketsMock = new Mock<ISharingTickets>();
        _apiMock = new Mock<SFA.DAS.Zendesk.Monitor.Middleware.IApi>();
        _watcherLoggerMock = new Mock<ILogger<Watcher>>();
        _loggerMock = new Mock<ILogger<DurableWatcher>>();

        _watcher = new Watcher(
            _sharingTicketsMock.Object,
            _apiMock.Object,
            _watcherLoggerMock.Object
        );

        _sut = new DurableWatcher(_watcher, _loggerMock.Object);
    }

[Fact]
    public async Task SearchTickets_ReturnsTicketsFromWatcher()
    {
        // Arrange
        var expected = new long[] { 1, 2, 3 };
        _sharingTicketsMock.Setup(s => s.GetTicketsForSharing())
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.SearchTickets("input");

        // Assert
        Assert.Equal(expected, result);
        _sharingTicketsMock.Verify(s => s.GetTicketsForSharing(), Times.Once);
    }

    [Fact]
    public async Task SearchTickets_ReturnsEmptyArray_WhenNull()
    {
        // Arrange
        _sharingTicketsMock.Setup(s => s.GetTicketsForSharing())
            .ReturnsAsync((long[]?)null);

        // Act
        var result = await _sut.SearchTickets("input");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _sharingTicketsMock.Verify(s => s.GetTicketsForSharing(), Times.Once);
    }

    [Fact]
    public async Task ShareTicket_CallsWatcherAndLogs()
    {
        // Arrange
        var ticketId = 42L;
        _sharingTicketsMock.Setup(s => s.GetTicketForSharing(ticketId))
            .ReturnsAsync(default(Option<SharedTicket>));

        // Act
        await _sut.ShareTicket(ticketId);

        // Assert
        _sharingTicketsMock.Verify(s => s.GetTicketForSharing(ticketId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(ticketId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNullWatcher()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DurableWatcher(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DurableWatcher(_watcher, null!));
    }
}

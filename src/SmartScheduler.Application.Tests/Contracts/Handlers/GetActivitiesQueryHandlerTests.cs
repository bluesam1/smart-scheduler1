using Moq;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Handlers;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Tests.Contracts.Handlers;

public class GetActivitiesQueryHandlerTests
{
    private readonly Mock<IEventLogRepository> _eventLogRepositoryMock;
    private readonly Mock<ILogger<GetActivitiesQueryHandler>> _loggerMock;
    private readonly GetActivitiesQueryHandler _handler;

    public GetActivitiesQueryHandlerTests()
    {
        _eventLogRepositoryMock = new Mock<IEventLogRepository>();
        _loggerMock = new Mock<ILogger<GetActivitiesQueryHandler>>();

        _handler = new GetActivitiesQueryHandler(
            _eventLogRepositoryMock.Object,
            _loggerMock.Object);
    }

    private EventLog CreateTestEventLog(string eventType, object payload, DateTime createdAt)
    {
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        return new EventLog(
            Guid.NewGuid(),
            eventType,
            payloadJson,
            DateTime.UtcNow,
            new[] { "dispatch/Default" });
    }

    [Fact]
    public async Task Handle_WithNoEvents_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 20 };

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithJobAssignedEvent_TransformsToAssignmentActivity()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 20 };
        var jobId = Guid.NewGuid().ToString();
        var contractorId = Guid.NewGuid().ToString();
        var payload = new
        {
            JobId = jobId,
            ContractorId = contractorId
        };
        var eventLog = CreateTestEventLog("JobAssigned", payload, DateTime.UtcNow);

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog> { eventLog });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("assignment", result[0].Type);
        Assert.Equal("Job Assigned", result[0].Title);
        Assert.Contains("assigned to contractor", result[0].Description);
    }

    [Fact]
    public async Task Handle_WithJobCancelledEvent_TransformsToCancellationActivity()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 20 };
        var jobId = Guid.NewGuid().ToString();
        var payload = new
        {
            JobId = jobId,
            Reason = "Customer cancelled"
        };
        var eventLog = CreateTestEventLog("JobCancelled", payload, DateTime.UtcNow);

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog> { eventLog });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("cancellation", result[0].Type);
        Assert.Equal("Job Cancelled", result[0].Title);
        Assert.Contains("was cancelled", result[0].Description);
    }

    [Fact]
    public async Task Handle_WithTypeFilter_FiltersByActivityType()
    {
        // Arrange
        var query = new GetActivitiesQuery
        {
            Types = new[] { "assignment" },
            Limit = 20
        };
        var jobAssignedEvent = CreateTestEventLog("JobAssigned", new { JobId = "1", ContractorId = "1" }, DateTime.UtcNow);
        var jobCancelledEvent = CreateTestEventLog("JobCancelled", new { JobId = "2", Reason = "Test" }, DateTime.UtcNow);

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, It.Is<IReadOnlyList<string>>(types => types.Contains("JobAssigned")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog> { jobAssignedEvent });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("assignment", result[0].Type);
    }

    [Fact]
    public async Task Handle_WithLimit_RespectsLimit()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 5 };
        var events = Enumerable.Range(0, 10)
            .Select(i => CreateTestEventLog("JobAssigned", new { JobId = i.ToString(), ContractorId = "1" }, DateTime.UtcNow))
            .ToList();

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events.Take(5).ToList());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task Handle_WithInvalidJson_ReturnsFallbackActivity()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 20 };
        // Create event log with invalid JSON
        var eventLog = new EventLog(
            Guid.NewGuid(),
            "JobAssigned",
            "invalid json {",
            DateTime.UtcNow,
            new[] { "dispatch/Default" });

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog> { eventLog });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("unknown", result[0].Type);
        Assert.Contains("Unable to parse", result[0].Description);
    }

    [Fact]
    public async Task Handle_WithUnknownEventType_ReturnsUnknownActivity()
    {
        // Arrange
        var query = new GetActivitiesQuery { Limit = 20 };
        var payload = new { Test = "data" };
        var eventLog = CreateTestEventLog("UnknownEventType", payload, DateTime.UtcNow);

        _eventLogRepositoryMock.Setup(r => r.GetRecentAsync(
            20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventLog> { eventLog });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("unknown", result[0].Type);
        Assert.Contains("UnknownEventType", result[0].Title);
    }
}





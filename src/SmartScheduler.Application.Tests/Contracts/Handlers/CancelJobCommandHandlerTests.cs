using Moq;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.Handlers;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Tests.Contracts.Handlers;

public class CancelJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private readonly Mock<IRealtimePublisher> _realtimePublisherMock;
    private readonly Mock<ICalendarConsistencyChecker> _consistencyCheckerMock;
    private readonly CancelJobCommandHandler _handler;

    public CancelJobCommandHandlerTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository>();
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _realtimePublisherMock = new Mock<IRealtimePublisher>();
        _consistencyCheckerMock = new Mock<ICalendarConsistencyChecker>();

        _handler = new CancelJobCommandHandler(
            _jobRepositoryMock.Object,
            _assignmentRepositoryMock.Object,
            _realtimePublisherMock.Object,
            _consistencyCheckerMock.Object);
    }

    private Job CreateTestJob(Guid id, JobStatus status = JobStatus.Scheduled)
    {
        var location = new GeoLocation(
            40.7128, -74.0060, "123 Main St", "New York", "NY", "10001", "US",
            "123 Main St, New York, NY 10001, USA", "ChIJexample");
        var serviceWindow = new TimeWindow(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));
        
        return new Job(
            id,
            "Test Job",
            240, // 4 hours
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(1),
            new List<string> { "Skill1" });
    }

    private Assignment CreateTestAssignment(Guid id, Guid jobId, Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        return new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Manual);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CancelsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Scheduled);
        var assignment = CreateTestAssignment(
            assignmentId,
            jobId,
            contractorId,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        var request = new CancelJobRequest
        {
            Reason = "Customer cancelled"
        };
        
        var command = new CancelJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jobId, result.Id);
        // Job status is updated via Cancel() method which sets it to Cancelled
        Assert.Equal("Cancelled", result.Status);
        
        _jobRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Once);
        _realtimePublisherMock.Verify(r => r.PublishJobCancelledAsync(
            jobId.ToString(),
            "Customer cancelled",
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoReason_UsesDefaultReason()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateTestJob(jobId, JobStatus.Scheduled);
        
        var command = new CancelJobCommand
        {
            JobId = jobId,
            Request = null
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _realtimePublisherMock.Verify(r => r.PublishJobCancelledAsync(
            jobId.ToString(),
            "No reason provided",
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentJob_ThrowsKeyNotFoundException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var command = new CancelJobCommand
        {
            JobId = jobId,
            Request = new CancelJobRequest { Reason = "Test" }
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithMultipleAssignments_CancelsAll()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId1 = Guid.NewGuid();
        var contractorId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Scheduled);
        var assignment1 = CreateTestAssignment(
            assignmentId1,
            jobId,
            contractorId1,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        var assignment2 = CreateTestAssignment(
            assignmentId2,
            jobId,
            contractorId2,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        var command = new CancelJobCommand
        {
            JobId = jobId,
            Request = new CancelJobRequest { Reason = "Test" }
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _assignmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _realtimePublisherMock.Verify(r => r.PublishJobCancelledAsync(
            jobId.ToString(),
            "Test",
            It.Is<IReadOnlyList<string>>(ids => ids.Count == 2),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompletedAssignment_DoesNotCancelCompleted()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Scheduled);
        var assignment = CreateTestAssignment(
            assignmentId,
            jobId,
            contractorId,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        // Mark assignment as completed (must be confirmed first, then in progress, then completed)
        assignment.Confirm();
        assignment.MarkInProgress();
        assignment.MarkCompleted();
        
        var command = new CancelJobCommand
        {
            JobId = jobId,
            Request = new CancelJobRequest { Reason = "Test" }
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Should not update completed assignments
        _assignmentRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Assignment>(a => a.Status == AssignmentEntityStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}


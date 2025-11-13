using Moq;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.Handlers;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Tests.Contracts.Handlers;

public class RescheduleJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private readonly Mock<IAvailabilityRevalidator> _availabilityRevalidatorMock;
    private readonly Mock<IContractorRepository> _contractorRepositoryMock;
    private readonly Mock<IRealtimePublisher> _realtimePublisherMock;
    private readonly Mock<ICalendarConsistencyChecker> _consistencyCheckerMock;
    private readonly RescheduleJobCommandHandler _handler;

    public RescheduleJobCommandHandlerTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository>();
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _availabilityRevalidatorMock = new Mock<IAvailabilityRevalidator>();
        _contractorRepositoryMock = new Mock<IContractorRepository>();
        _realtimePublisherMock = new Mock<IRealtimePublisher>();
        _consistencyCheckerMock = new Mock<ICalendarConsistencyChecker>();

        _handler = new RescheduleJobCommandHandler(
            _jobRepositoryMock.Object,
            _assignmentRepositoryMock.Object,
            _availabilityRevalidatorMock.Object,
            _contractorRepositoryMock.Object,
            _realtimePublisherMock.Object,
            _consistencyCheckerMock.Object);
    }

    private Job CreateTestJob(Guid id, JobStatus status = JobStatus.Assigned)
    {
        var location = new GeoLocation(
            40.7128, -74.0060, "123 Main St", "New York", "NY", "10001", "US",
            "123 Main St, New York, NY 10001, USA", "ChIJexample");
        var serviceWindow = new TimeWindow(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));
        
        var job = new Job(
            id,
            "Test Job",
            240, // 4 hours
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(1),
            new List<string> { "Skill1" });
        
        // Set status through valid transitions
        if (status == JobStatus.Assigned)
        {
            job.UpdateStatus(JobStatus.Assigned);
        }
        else if (status == JobStatus.Completed)
        {
            job.UpdateStatus(JobStatus.Assigned);
            job.UpdateStatus(JobStatus.InProgress);
            job.UpdateStatus(JobStatus.Completed);
        }
        else if (status == JobStatus.Cancelled)
        {
            job.Cancel("Test cancellation");
        }
        
        return job;
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
    public async Task Handle_WithValidRequest_ReschedulesJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Assigned);
        var previousStart = job.ServiceWindow.Start;
        var previousEnd = job.ServiceWindow.End;
        
        var assignment = CreateTestAssignment(
            assignmentId,
            jobId,
            contractorId,
            previousStart,
            previousEnd);
        
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = newStartUtc.AddHours(4);
        
        var request = new RescheduleJobRequest
        {
            StartUtc = newStartUtc,
            EndUtc = newEndUtc
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAndTimeRangeAsync(
            contractorId, newStartUtc, newEndUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());
        
        _availabilityRevalidatorMock.Setup(r => r.ValidateAvailabilityAsync(
            contractorId, jobId, newStartUtc, It.IsAny<DateTime>(), 240, "America/New_York", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AvailabilityValidationResult.Valid());
        
        var contractorLocation = new GeoLocation(
            40.7128, -74.0060, "123 Main St", "New York", "NY", "10001", "US",
            "123 Main St, New York, NY 10001, USA", "ChIJexample");
        var contractorWorkingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };
        
        _contractorRepositoryMock.Setup(r => r.GetByIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contractor(
                contractorId,
                "Test Contractor",
                contractorLocation,
                contractorWorkingHours,
                new List<string> { "Skill1" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jobId, result.Id);
        Assert.Equal(newStartUtc, result.ServiceWindow.Start);
        Assert.Equal(newEndUtc, result.ServiceWindow.End);
        
        _jobRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Once);
        _realtimePublisherMock.Verify(r => r.PublishJobRescheduledAsync(
            jobId.ToString(),
            previousStart,
            previousEnd,
            newStartUtc,
            newEndUtc,
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidTimeSlot_ThrowsArgumentException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var request = new RescheduleJobRequest
        {
            StartUtc = startTime,
            EndUtc = startTime.AddHours(-1) // End before start (invalid)
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        // Act & Assert
        // Should throw ArgumentException before even checking if job exists
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNonExistentJob_ThrowsKeyNotFoundException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var request = new RescheduleJobRequest
        {
            StartUtc = DateTime.UtcNow.AddDays(1),
            EndUtc = DateTime.UtcNow.AddDays(1).AddHours(4)
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCompletedJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateTestJob(jobId, JobStatus.Completed);
        
        var request = new RescheduleJobRequest
        {
            StartUtc = DateTime.UtcNow.AddDays(1),
            EndUtc = DateTime.UtcNow.AddDays(1).AddHours(4)
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("completed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithCancelledJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateTestJob(jobId, JobStatus.Cancelled);
        
        var request = new RescheduleJobRequest
        {
            StartUtc = DateTime.UtcNow.AddDays(1),
            EndUtc = DateTime.UtcNow.AddDays(1).AddHours(4)
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithUnavailableContractor_ThrowsInvalidOperationException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Assigned);
        var assignment = CreateTestAssignment(
            assignmentId,
            jobId,
            contractorId,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = newStartUtc.AddHours(4);
        
        var request = new RescheduleJobRequest
        {
            StartUtc = newStartUtc,
            EndUtc = newEndUtc
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });
        
        _availabilityRevalidatorMock.Setup(r => r.ValidateAvailabilityAsync(
            contractorId, jobId, newStartUtc, It.IsAny<DateTime>(), 240, "America/New_York", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AvailabilityValidationResult.Invalid("Contractor not available"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("not available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithConflictingAssignment_ThrowsInvalidOperationException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var conflictingAssignmentId = Guid.NewGuid();
        
        var job = CreateTestJob(jobId, JobStatus.Assigned);
        var assignment = CreateTestAssignment(
            assignmentId,
            jobId,
            contractorId,
            job.ServiceWindow.Start,
            job.ServiceWindow.End);
        
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = newStartUtc.AddHours(4);
        
        var conflictingAssignment = CreateTestAssignment(
            conflictingAssignmentId,
            Guid.NewGuid(), // Different job
            contractorId,
            newStartUtc,
            newEndUtc);
        
        var request = new RescheduleJobRequest
        {
            StartUtc = newStartUtc,
            EndUtc = newEndUtc
        };
        
        var command = new RescheduleJobCommand
        {
            JobId = jobId,
            Request = request
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _assignmentRepositoryMock.Setup(r => r.GetByJobIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAndTimeRangeAsync(
            contractorId, newStartUtc, newEndUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { conflictingAssignment });
        
        _availabilityRevalidatorMock.Setup(r => r.ValidateAvailabilityAsync(
            contractorId, jobId, newStartUtc, It.IsAny<DateTime>(), 240, "America/New_York", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AvailabilityValidationResult.Valid());
        
        var contractorLocation = new GeoLocation(
            40.7128, -74.0060, "123 Main St", "New York", "NY", "10001", "US",
            "123 Main St, New York, NY 10001, USA", "ChIJexample");
        var contractorWorkingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };
        
        _contractorRepositoryMock.Setup(r => r.GetByIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contractor(
                contractorId,
                "Test Contractor",
                contractorLocation,
                contractorWorkingHours,
                new List<string> { "Skill1" }));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("conflicting", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}


using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Handlers;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Tests.Contracts.Handlers;

public class GetDashboardStatisticsQueryHandlerTests
{
    private readonly Mock<IContractorRepository> _contractorRepositoryMock;
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<GetDashboardStatisticsQueryHandler>> _loggerMock;
    private readonly GetDashboardStatisticsQueryHandler _handler;

    public GetDashboardStatisticsQueryHandlerTests()
    {
        _contractorRepositoryMock = new Mock<IContractorRepository>();
        _jobRepositoryMock = new Mock<IJobRepository>();
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<GetDashboardStatisticsQueryHandler>>();

        _handler = new GetDashboardStatisticsQueryHandler(
            _contractorRepositoryMock.Object,
            _jobRepositoryMock.Object,
            _assignmentRepositoryMock.Object,
            _cache,
            _loggerMock.Object);
    }

    private Contractor CreateTestContractor(Guid id, DateTime createdAt)
    {
        var location = new GeoLocation(
            40.7128, -74.0060, "123 Main St", "New York", "NY", "10001", "US",
            "123 Main St, New York, NY 10001, USA", "ChIJexample");
        
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        return new Contractor(
            id,
            "Test Contractor",
            location,
            workingHours,
            new List<string> { "Skill1" },
            50);
    }

    private Job CreateTestJob(Guid id, JobStatus status)
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
        
        // Update status if needed (Job constructor sets it to Scheduled)
        if (status != JobStatus.Scheduled)
        {
            job.UpdateStatus(status);
        }
        
        return job;
    }

    private Assignment CreateTestAssignment(Guid id, Guid jobId, Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        var assignment = new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Manual);
        
        return assignment;
    }

    [Fact]
    public async Task Handle_WithNoData_ReturnsZeroStatistics()
    {
        // Arrange
        var query = new GetDashboardStatisticsQuery();

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor>());
        
        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());
        
        _assignmentRepositoryMock.Setup(r => r.GetActiveAssignmentsByContractorIdsAsync(
            It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ActiveContractors.Value);
        Assert.Equal(0, result.PendingJobs.Value);
        Assert.Equal(0, result.AverageAssignmentTime.ValueMinutes);
        Assert.Equal(0.0, result.UtilizationRate.Value);
    }

    [Fact]
    public async Task Handle_WithContractors_ReturnsCorrectCount()
    {
        // Arrange
        var query = new GetDashboardStatisticsQuery();
        var contractor1 = CreateTestContractor(Guid.NewGuid(), DateTime.UtcNow.AddDays(-10));
        var contractor2 = CreateTestContractor(Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
        var contractor3 = CreateTestContractor(Guid.NewGuid(), DateTime.UtcNow);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor1, contractor2, contractor3 });
        
        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());
        
        _assignmentRepositoryMock.Setup(r => r.GetActiveAssignmentsByContractorIdsAsync(
            It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.ActiveContractors.Value);
    }

    [Fact]
    public async Task Handle_WithPendingJobs_ReturnsCorrectCount()
    {
        // Arrange
        var query = new GetDashboardStatisticsQuery();
        var job1 = CreateTestJob(Guid.NewGuid(), JobStatus.Scheduled);
        var job2 = CreateTestJob(Guid.NewGuid(), JobStatus.Scheduled);
        var job3 = CreateTestJob(Guid.NewGuid(), JobStatus.Scheduled);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor>());
        
        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job1, job2, job3 });
        
        _assignmentRepositoryMock.Setup(r => r.GetActiveAssignmentsByContractorIdsAsync(
            It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PendingJobs.Value); // Only Created status jobs
    }

    [Fact]
    public async Task Handle_WithCachedData_ReturnsCachedStatistics()
    {
        // Arrange
        var query = new GetDashboardStatisticsQuery();
        var cachedStats = new DashboardStatisticsDto
        {
            ActiveContractors = new StatMetric { Value = 10 },
            PendingJobs = new JobStatMetric { Value = 5, Unassigned = 3 },
            AverageAssignmentTime = new TimeMetric { ValueMinutes = 30 },
            UtilizationRate = new PercentMetric { Value = 75.0 }
        };

        _cache.Set("dashboard_statistics", cachedStats, TimeSpan.FromMinutes(5));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.ActiveContractors.Value);
        Assert.Equal(5, result.PendingJobs.Value);
        
        // Verify repositories were not called
        _contractorRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAssignments_CalculatesUtilization()
    {
        // Arrange
        var query = new GetDashboardStatisticsQuery();
        var contractorId = Guid.NewGuid();
        var contractor = CreateTestContractor(contractorId, DateTime.UtcNow);
        
        var assignmentId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow;
        var endUtc = startUtc.AddHours(2); // 2 hours = 120 minutes
        var assignment = CreateTestAssignment(assignmentId, jobId, contractorId, startUtc, endUtc);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor });
        
        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());
        
        _assignmentRepositoryMock.Setup(r => r.GetActiveAssignmentsByContractorIdsAsync(
            It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(contractorId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Utilization should be calculated: 120 minutes assigned / available time
        // Contractor has 1 working day, 8 hours = 480 minutes per week
        // Utilization = 120 / 480 * 100 = 25%
        Assert.True(result.UtilizationRate.Value > 0);
    }
}


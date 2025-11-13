using Moq;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.DTOs;
using SmartScheduler.Application.Recommendations.Handlers;
using SmartScheduler.Application.Recommendations.Queries;
using SmartScheduler.Application.Recommendations.Services;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using System.Text.Json;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations;

public class GetRecommendationsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly Mock<IContractorRepository> _contractorRepositoryMock;
    private readonly Mock<IAvailabilityEngine> _availabilityEngineMock;
    private readonly Mock<IScoringService> _scoringServiceMock;
    private readonly Mock<ISlotGenerator> _slotGeneratorMock;
    private readonly Mock<IRationaleGenerator> _rationaleGeneratorMock;
    private readonly Mock<IRotationBoostService> _rotationBoostServiceMock;
    private readonly Mock<IConfigLoader> _configLoaderMock;
    private readonly Mock<IRecommendationAuditRepository> _auditRepositoryMock;
    private readonly Mock<ILogger<GetRecommendationsQueryHandler>> _loggerMock;
    private readonly GetRecommendationsQueryHandler _handler;

    public GetRecommendationsQueryHandlerTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository>();
        _contractorRepositoryMock = new Mock<IContractorRepository>();
        _availabilityEngineMock = new Mock<IAvailabilityEngine>();
        _scoringServiceMock = new Mock<IScoringService>();
        _slotGeneratorMock = new Mock<ISlotGenerator>();
        _rationaleGeneratorMock = new Mock<IRationaleGenerator>();
        _rotationBoostServiceMock = new Mock<IRotationBoostService>();
        _configLoaderMock = new Mock<IConfigLoader>();
        _auditRepositoryMock = new Mock<IRecommendationAuditRepository>();
        _loggerMock = new Mock<ILogger<GetRecommendationsQueryHandler>>();

        // Setup default config
        _configLoaderMock.Setup(c => c.GetConfig())
            .Returns(new RecommendationConfig { Version = 1 });

        _handler = new GetRecommendationsQueryHandler(
            _jobRepositoryMock.Object,
            _contractorRepositoryMock.Object,
            _availabilityEngineMock.Object,
            _scoringServiceMock.Object,
            _slotGeneratorMock.Object,
            _rationaleGeneratorMock.Object,
            _rotationBoostServiceMock.Object,
            _configLoaderMock.Object,
            _auditRepositoryMock.Object,
            _loggerMock.Object);
    }

    private Job CreateTestJob(string id, int durationMinutes = 120)
    {
        var jobId = Guid.Parse(id);
        var location = new GeoLocation(
            40.7128, // NYC coordinates
            -74.0060,
            "123 Main St",
            "New York",
            "NY",
            "10001",
            "US");

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 20, 13, 0, 0, DateTimeKind.Utc), // 8 AM ET
            new DateTime(2025, 1, 20, 22, 0, 0, DateTimeKind.Utc)); // 5 PM ET

        return new Job(
            jobId,
            "Flooring Installation",
            "Install hardwood floors",
            durationMinutes,
            location,
            serviceWindow,
            Priority.Normal,
            new List<string> { "Flooring", "Installation" },
            "America/New_York",
            null,
            DateTime.UtcNow);
    }

    private Contractor CreateTestContractor(string id, double lat, double lon)
    {
        var contractorId = Guid.Parse(id);
        var baseLocation = new GeoLocation(lat, lon, "456 Oak Ave", "Brooklyn", "NY", "11201", "US");
        
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(
                Guid.NewGuid(),
                contractorId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                "America/New_York")
        };

        return new Contractor(
            contractorId,
            $"Contractor {id}",
            baseLocation,
            workingHours,
            new List<string> { "Flooring", "Installation" },
            75,
            "America/New_York",
            null);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsException()
    {
        // Arrange
        var query = new GetRecommendationsQuery
        {
            JobId = Guid.NewGuid().ToString(),
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoContractors_ReturnsEmptyRecommendations()
    {
        // Arrange
        var jobId = "00000000-0000-0000-0000-000000000001";
        var job = CreateTestJob(jobId);

        var query = new GetRecommendationsQuery
        {
            JobId = jobId,
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor>());

        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public async Task Handle_ContractorWithNoAvailability_ExcludesFromRecommendations()
    {
        // Arrange
        var jobId = "00000000-0000-0000-0000-000000000001";
        var contractorId = "00000000-0000-0000-0000-000000000002";
        
        var job = CreateTestJob(jobId);
        var contractor = CreateTestContractor(contractorId, 40.7128, -74.0060);

        var query = new GetRecommendationsQuery
        {
            JobId = jobId,
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor });

        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        // No available slots
        _availabilityEngineMock.Setup(a => a.CalculateAvailableSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>()))
            .Returns(new List<TimeWindow>()); // Empty!

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public async Task Handle_ContractorWithAvailability_IncludesInRecommendations()
    {
        // Arrange
        var jobId = "00000000-0000-0000-0000-000000000001";
        var contractorId = "00000000-0000-0000-0000-000000000002";
        
        var job = CreateTestJob(jobId);
        var contractor = CreateTestContractor(contractorId, 40.7128, -74.0060);

        var query = new GetRecommendationsQuery
        {
            JobId = jobId,
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor });

        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        // Has available slots
        var availableSlot = new TimeWindow(
            new DateTime(2025, 1, 20, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 20, 18, 0, 0, DateTimeKind.Utc));
        
        _availabilityEngineMock.Setup(a => a.CalculateAvailableSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>()))
            .Returns(new List<TimeWindow> { availableSlot });

        // Setup scoring service
        _scoringServiceMock.Setup(s => s.CalculateScore(
            It.IsAny<int>(),
            It.IsAny<List<TimeWindow>>(),
            It.IsAny<double>(),
            It.IsAny<int>()))
            .Returns(new ScoringResult
            {
                FinalScore = 85.5,
                Breakdown = new ScoreBreakdown
                {
                    Availability = 90,
                    Rating = 75,
                    Distance = 80,
                    Rotation = 95
                }
            });

        // Setup slot generator to return suggested slots
        var suggestedSlot = new GeneratedSlot
        {
            Window = new TimeWindow(
                new DateTime(2025, 1, 20, 14, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 20, 16, 30, 0, DateTimeKind.Utc)),
            Type = SlotType.Earliest,
            Confidence = 85
        };

        _slotGeneratorMock.Setup(s => s.GenerateSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>(),
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .Returns(new List<GeneratedSlot> { suggestedSlot });

        _rationaleGeneratorMock.Setup(r => r.GenerateRationale(
            It.IsAny<ScoreBreakdown>(),
            It.IsAny<double>()))
            .Returns("Strong availability and good rating");

        _rotationBoostServiceMock.Setup(r => r.CalculateBoost(It.IsAny<double>()))
            .Returns(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Recommendations);
        Assert.Single(result.Recommendations);
        
        var recommendation = result.Recommendations[0];
        Assert.Equal(contractor.Id, recommendation.ContractorId);
        Assert.Equal(contractor.Name, recommendation.ContractorName);
        Assert.Equal(85.5, recommendation.Score);
        Assert.NotEmpty(recommendation.SuggestedSlots);
        Assert.Single(recommendation.SuggestedSlots);
        
        var slot = recommendation.SuggestedSlots[0];
        Assert.Equal(suggestedSlot.Window.Start, slot.StartUtc);
        Assert.Equal(suggestedSlot.Window.End, slot.EndUtc);
        Assert.Equal("earliest", slot.Type);
        Assert.Equal(85, slot.Confidence);
    }

    [Fact]
    public async Task Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots()
    {
        // Arrange
        var jobId = "00000000-0000-0000-0000-000000000001";
        var contractorId = "00000000-0000-0000-0000-000000000002";
        
        var job = CreateTestJob(jobId);
        var contractor = CreateTestContractor(contractorId, 40.7128, -74.0060);

        var query = new GetRecommendationsQuery
        {
            JobId = jobId,
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor });

        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        // Has available slots from availability engine
        var availableSlot = new TimeWindow(
            new DateTime(2025, 1, 20, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 20, 18, 0, 0, DateTimeKind.Utc));
        
        _availabilityEngineMock.Setup(a => a.CalculateAvailableSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>()))
            .Returns(new List<TimeWindow> { availableSlot });

        _scoringServiceMock.Setup(s => s.CalculateScore(
            It.IsAny<int>(),
            It.IsAny<List<TimeWindow>>(),
            It.IsAny<double>(),
            It.IsAny<int>()))
            .Returns(new ScoringResult
            {
                FinalScore = 85.5,
                Breakdown = new ScoreBreakdown
                {
                    Availability = 90,
                    Rating = 75,
                    Distance = 80,
                    Rotation = 95
                }
            });

        // Slot generator returns EMPTY list (this is the issue we're testing)
        _slotGeneratorMock.Setup(s => s.GenerateSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>(),
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .Returns(new List<GeneratedSlot>()); // EMPTY!

        _rationaleGeneratorMock.Setup(r => r.GenerateRationale(
            It.IsAny<ScoreBreakdown>(),
            It.IsAny<double>()))
            .Returns("Has availability but no specific time slots suggested");

        _rotationBoostServiceMock.Setup(r => r.CalculateBoost(It.IsAny<double>()))
            .Returns(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Recommendations);
        Assert.Single(result.Recommendations);
        
        var recommendation = result.Recommendations[0];
        Assert.Equal(contractor.Id, recommendation.ContractorId);
        Assert.NotNull(recommendation.SuggestedSlots);
        Assert.Empty(recommendation.SuggestedSlots); // Should be empty array, not null
    }

    [Fact]
    public async Task Handle_MultipleContractors_RanksByScore()
    {
        // Arrange
        var jobId = "00000000-0000-0000-0000-000000000001";
        var contractor1Id = "00000000-0000-0000-0000-000000000002";
        var contractor2Id = "00000000-0000-0000-0000-000000000003";
        
        var job = CreateTestJob(jobId);
        var contractor1 = CreateTestContractor(contractor1Id, 40.7128, -74.0060);
        var contractor2 = CreateTestContractor(contractor2Id, 40.7580, -73.9855);

        var query = new GetRecommendationsQuery
        {
            JobId = jobId,
            DesiredDate = "2025-01-20",
            MaxResults = 10
        };

        _jobRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _contractorRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contractor> { contractor1, contractor2 });

        _jobRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var availableSlot = new TimeWindow(
            new DateTime(2025, 1, 20, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 20, 18, 0, 0, DateTimeKind.Utc));
        
        _availabilityEngineMock.Setup(a => a.CalculateAvailableSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>()))
            .Returns(new List<TimeWindow> { availableSlot });

        // Contractor 1: Higher score
        _scoringServiceMock.Setup(s => s.CalculateScore(
            It.IsAny<int>(),
            It.IsAny<List<TimeWindow>>(),
            It.IsAny<double>(),
            It.IsAny<int>()))
            .Returns((int rating, List<TimeWindow> slots, double dist, int boost) =>
            {
                // Return different scores to simulate real ranking
                return new ScoringResult
                {
                    FinalScore = rating == 75 ? 90.0 : 75.0, // First contractor gets higher score
                    Breakdown = new ScoreBreakdown
                    {
                        Availability = 90,
                        Rating = rating,
                        Distance = 80,
                        Rotation = 95
                    }
                };
            });

        _slotGeneratorMock.Setup(s => s.GenerateSlots(
            It.IsAny<List<WorkingHours>>(),
            It.IsAny<TimeWindow>(),
            It.IsAny<IEnumerable<TimeWindow>?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ContractorCalendar?>(),
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<int>(),
            It.IsAny<bool>()))
            .Returns(new List<GeneratedSlot>
            {
                new GeneratedSlot
                {
                    Window = new TimeWindow(
                        new DateTime(2025, 1, 20, 14, 30, 0, DateTimeKind.Utc),
                        new DateTime(2025, 1, 20, 16, 30, 0, DateTimeKind.Utc)),
                    Type = SlotType.Earliest,
                    Confidence = 85
                }
            });

        _rationaleGeneratorMock.Setup(r => r.GenerateRationale(
            It.IsAny<ScoreBreakdown>(),
            It.IsAny<double>()))
            .Returns("Good match");

        _rotationBoostServiceMock.Setup(r => r.CalculateBoost(It.IsAny<double>()))
            .Returns(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Recommendations.Count);
        
        // Should be ranked by score (highest first)
        Assert.True(result.Recommendations[0].Score >= result.Recommendations[1].Score);
    }
}


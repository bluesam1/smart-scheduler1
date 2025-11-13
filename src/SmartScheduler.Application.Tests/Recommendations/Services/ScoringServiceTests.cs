using Microsoft.Extensions.Logging;
using SmartScheduler.Application.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Application.Recommendations.Services;
using SmartScheduler.Domain.Contracts.ValueObjects;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations.Services;

public class ScoringServiceTests
{
    private readonly IScoringService _scoringService;
    private readonly MockConfigLoader _mockConfigLoader;

    public ScoringServiceTests()
    {
        _mockConfigLoader = new MockConfigLoader();
        var logger = new LoggerFactory().CreateLogger<ScoringService>();
        _scoringService = new ScoringService(_mockConfigLoader, logger);
    }

    [Fact]
    public void CalculateScore_WithValidInputs_ReturnsScoreInRange()
    {
        // Arrange
        var rating = 80;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)),
            new(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(5))
        };
        var distance = 5000; // 5km

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.FinalScore >= 0 && result.FinalScore <= 100);
        Assert.NotNull(result.Breakdown);
    }

    [Fact]
    public void CalculateScore_WithZeroDistance_ReturnsHighDistanceScore()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 0; // Zero distance

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.Equal(100, result.Breakdown.Distance);
    }

    [Fact]
    public void CalculateScore_WithNoAvailableSlots_ReturnsZeroAvailability()
    {
        // Arrange
        var rating = 80;
        var slots = Array.Empty<TimeWindow>();
        var distance = 5000;

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.Equal(0, result.Breakdown.Availability);
    }

    [Fact]
    public void CalculateScore_WithHighRating_ReturnsHighRatingScore()
    {
        // Arrange
        var rating = 95;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 5000;

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.Equal(95, result.Breakdown.Rating);
    }

    [Fact]
    public void CalculateScore_WithManySlots_ReturnsHighAvailabilityScore()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>();
        for (int i = 0; i < 10; i++)
        {
            slots.Add(new TimeWindow(
                DateTime.UtcNow.AddHours(i),
                DateTime.UtcNow.AddHours(i + 2)));
        }
        var distance = 5000;

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.True(result.Breakdown.Availability > 50);
    }

    [Fact]
    public void CalculateScore_WithLongDistance_ReturnsLowDistanceScore()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 50000; // 50km

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.True(result.Breakdown.Distance < 50);
    }

    [Fact]
    public void CalculateScore_WithRotationBoost_IncludesBoost()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 5000;
        var rotationBoost = 5.0;

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance, rotationBoost);

        // Assert
        Assert.NotNull(result.Breakdown.Rotation);
        Assert.Equal(5.0, result.Breakdown.Rotation);
    }

    [Fact]
    public void CalculateScore_IsDeterministic_SameInputsProduceSameOutputs()
    {
        // Arrange
        var rating = 75;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 10000;

        // Act
        var result1 = _scoringService.CalculateScore(rating, slots, distance);
        var result2 = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.Equal(result1.FinalScore, result2.FinalScore, 5); // Allow small floating point differences
        Assert.Equal(result1.Breakdown.Availability, result2.Breakdown.Availability, 5);
        Assert.Equal(result1.Breakdown.Rating, result2.Breakdown.Rating);
        Assert.Equal(result1.Breakdown.Distance, result2.Breakdown.Distance, 5);
    }

    [Fact]
    public void CalculateScore_WithNegativeDistance_HandlesGracefully()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = -100; // Invalid distance

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        // Should handle gracefully (treats as 0)
        Assert.True(result.Breakdown.Distance >= 0);
    }

    [Fact]
    public void CalculateScore_WithVeryLongDistance_ReturnsMinimumDistanceScore()
    {
        // Arrange
        var rating = 50;
        var slots = new List<TimeWindow>
        {
            new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2))
        };
        var distance = 200000; // 200km - beyond max

        // Act
        var result = _scoringService.CalculateScore(rating, slots, distance);

        // Assert
        Assert.Equal(0, result.Breakdown.Distance);
    }

    // Mock config loader for testing
    private class MockConfigLoader : IScoringWeightsConfigLoader
    {
        public ScoringWeightsConfig GetConfig()
        {
            return new ScoringWeightsConfig
            {
                Version = 1,
                Weights = new WeightFactors
                {
                    Availability = 0.20,
                    Rating = 0.35,
                    Distance = 0.45
                },
                Rotation = new RotationConfig
                {
                    Enabled = true,
                    Boost = 3.0,
                    UnderUtilizationThreshold = 0.20
                }
            };
        }

        public int GetConfigVersion()
        {
            return 1;
        }
    }
}



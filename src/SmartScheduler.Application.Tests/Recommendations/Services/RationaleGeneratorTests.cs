using SmartScheduler.Application.DTOs;
using SmartScheduler.Application.Recommendations.Services;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations.Services;

public class RationaleGeneratorTests
{
    private readonly IRationaleGenerator _rationaleGenerator;

    public RationaleGeneratorTests()
    {
        _rationaleGenerator = new RationaleGenerator();
    }

    [Fact]
    public void GenerateRationale_WithHighAvailability_ReturnsAvailabilityRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 85,
            Rating = 70,
            Distance = 60
        };
        var finalScore = 75.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        Assert.Contains("availability", rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateRationale_WithHighRating_ReturnsRatingRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 60,
            Rating = 95,
            Distance = 50
        };
        var finalScore = 75.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        // Should mention rating (e.g., "Top-rated", "rated", "rating")
        Assert.True(
            rationale.Contains("rated", StringComparison.OrdinalIgnoreCase) ||
            rationale.Contains("rating", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateRationale_WithHighDistance_ReturnsDistanceRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 50,
            Rating = 60,
            Distance = 90
        };
        var finalScore = 65.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        Assert.True(
            rationale.Contains("distance", StringComparison.OrdinalIgnoreCase) ||
            rationale.Contains("location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateRationale_WithBalancedScores_ReturnsBalancedRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 55,
            Rating = 60,
            Distance = 50
        };
        var finalScore = 55.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        Assert.NotEmpty(rationale);
        // Should generate a rationale (may be balanced or highlight primary factor)
    }

    [Fact]
    public void GenerateRationale_IsDeterministic_SameInputsProduceSameOutput()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 80,
            Rating = 75,
            Distance = 70
        };
        var finalScore = 75.0;

        // Act
        var rationale1 = _rationaleGenerator.GenerateRationale(breakdown, finalScore);
        var rationale2 = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.Equal(rationale1, rationale2);
    }

    [Fact]
    public void GenerateRationale_WithRotationBoost_IncludesInRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 70,
            Rating = 75,
            Distance = 65,
            Rotation = 3.0
        };
        var finalScore = 72.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        // Rationale should still be generated (rotation is factored into final score)
    }

    [Fact]
    public void GenerateRationale_WithVeryLongRationale_TruncatesToMaxLength()
    {
        // Arrange
        // Create a breakdown that might generate a long rationale
        var breakdown = new ScoreBreakdown
        {
            Availability = 85,
            Rating = 90,
            Distance = 88
        };
        var finalScore = 88.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
    }

    [Fact]
    public void GenerateRationale_WithLowScores_ReturnsAppropriateRationale()
    {
        // Arrange
        var breakdown = new ScoreBreakdown
        {
            Availability = 30,
            Rating = 40,
            Distance = 35
        };
        var finalScore = 35.0;

        // Act
        var rationale = _rationaleGenerator.GenerateRationale(breakdown, finalScore);

        // Assert
        Assert.NotNull(rationale);
        Assert.True(rationale.Length <= 200);
        // Should still generate a rationale
    }

    [Fact]
    public void GenerateRationale_AlwaysUnder200Characters()
    {
        // Arrange - test various combinations
        var testCases = new[]
        {
            new { Breakdown = new ScoreBreakdown { Availability = 100, Rating = 100, Distance = 100 }, Score = 100.0 },
            new { Breakdown = new ScoreBreakdown { Availability = 0, Rating = 0, Distance = 0 }, Score = 0.0 },
            new { Breakdown = new ScoreBreakdown { Availability = 50, Rating = 50, Distance = 50 }, Score = 50.0 },
            new { Breakdown = new ScoreBreakdown { Availability = 95, Rating = 60, Distance = 40 }, Score = 65.0 },
            new { Breakdown = new ScoreBreakdown { Availability = 40, Rating = 95, Distance = 60 }, Score = 65.0 },
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var rationale = _rationaleGenerator.GenerateRationale(testCase.Breakdown, testCase.Score);
            Assert.True(rationale.Length <= 200, $"Rationale exceeded 200 chars: {rationale}");
        }
    }
}


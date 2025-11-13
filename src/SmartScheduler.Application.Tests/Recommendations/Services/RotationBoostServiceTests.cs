using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Application.Recommendations.Services;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations.Services;

public class RotationBoostServiceTests
{
    private readonly IRotationBoostService _rotationBoostService;
    private readonly MockConfigLoader _mockConfigLoader;

    public RotationBoostServiceTests()
    {
        _mockConfigLoader = new MockConfigLoader();
        var logger = new LoggerFactory().CreateLogger<RotationBoostService>();
        _rotationBoostService = new RotationBoostService(_mockConfigLoader, logger);
    }

    [Fact]
    public void CalculateBoost_WithLowUtilization_ReturnsBoost()
    {
        // Arrange
        var utilization = 0.10; // 10% utilization (below 20% threshold)

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.NotNull(boost);
        Assert.True(boost > 0);
        Assert.True(boost <= 3.0); // Should be <= configured boost (3.0)
    }

    [Fact]
    public void CalculateBoost_WithZeroUtilization_ReturnsMaxBoost()
    {
        // Arrange
        var utilization = 0.0;

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.NotNull(boost);
        Assert.True(Math.Abs(3.0 - boost.Value) < 0.1); // Should be close to max boost (3.0)
    }

    [Fact]
    public void CalculateBoost_WithHighUtilization_ReturnsNull()
    {
        // Arrange
        var utilization = 0.50; // 50% utilization (above 20% threshold)

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.Null(boost); // No boost for adequately utilized contractors
    }

    [Fact]
    public void CalculateBoost_AtThreshold_ReturnsZero()
    {
        // Arrange
        var utilization = 0.20; // Exactly at threshold (20%)

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.Null(boost); // No boost at threshold
    }

    [Fact]
    public void CalculateBoost_WithUtilizationDecay_ReturnsProportionalBoost()
    {
        // Arrange
        var utilization1 = 0.0;  // 0% - should get max boost
        var utilization2 = 0.10;  // 10% - should get half boost
        var utilization3 = 0.20;  // 20% - should get no boost

        // Act
        var boost1 = _rotationBoostService.CalculateBoost(utilization1);
        var boost2 = _rotationBoostService.CalculateBoost(utilization2);
        var boost3 = _rotationBoostService.CalculateBoost(utilization3);

        // Assert
        Assert.NotNull(boost1);
        Assert.NotNull(boost2);
        Assert.Null(boost3);
        Assert.True(boost1 > boost2); // Higher boost for lower utilization
    }

    [Fact]
    public void CalculateBoost_WithDisabledRotation_ReturnsNull()
    {
        // Arrange
        _mockConfigLoader.SetRotationEnabled(false);
        var utilization = 0.10;

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.Null(boost); // No boost when rotation is disabled
    }

    [Fact]
    public void CalculateBoost_WithInvalidUtilization_HandlesGracefully()
    {
        // Arrange
        var utilization = -0.5; // Invalid negative value

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        // Should handle gracefully (clamp to 0.0)
        Assert.NotNull(boost); // Should still calculate boost for 0% utilization
    }

    [Fact]
    public void CalculateBoost_WithOverOneUtilization_HandlesGracefully()
    {
        // Arrange
        var utilization = 1.5; // Invalid > 1.0 value

        // Act
        var boost = _rotationBoostService.CalculateBoost(utilization);

        // Assert
        Assert.Null(boost); // Should clamp to 1.0, which is above threshold
    }

    // Mock config loader for testing
    private class MockConfigLoader : IScoringWeightsConfigLoader
    {
        private bool _rotationEnabled = true;

        public void SetRotationEnabled(bool enabled)
        {
            _rotationEnabled = enabled;
        }

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
                    Enabled = _rotationEnabled,
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


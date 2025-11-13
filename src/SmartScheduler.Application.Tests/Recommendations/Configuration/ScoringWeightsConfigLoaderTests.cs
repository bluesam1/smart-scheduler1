using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Infrastructure.Configuration;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations.Configuration;

public class ScoringWeightsConfigLoaderTests
{
    [Fact]
    public void GetConfig_WithValidConfiguration_ReturnsConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "1" },
                { "ScoringWeights:Weights:Availability", "0.20" },
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" },
                { "ScoringWeights:TieBreakers:0", "earliestStart" },
                { "ScoringWeights:TieBreakers:1", "lowerDayUtilization" },
                { "ScoringWeights:TieBreakers:2", "shortestNextLeg" },
                { "ScoringWeights:Rotation:Enabled", "true" },
                { "ScoringWeights:Rotation:Boost", "3.0" },
                { "ScoringWeights:Rotation:UnderUtilizationThreshold", "0.20" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act
        var config = loader.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(1, config.Version);
        Assert.Equal(0.20, config.Weights.Availability);
        Assert.Equal(0.35, config.Weights.Rating);
        Assert.Equal(0.45, config.Weights.Distance);
        Assert.Equal(3, config.TieBreakers.Count);
        Assert.True(config.Rotation.Enabled);
        Assert.Equal(3.0, config.Rotation.Boost);
        Assert.Equal(0.20, config.Rotation.UnderUtilizationThreshold);
    }

    [Fact]
    public void GetConfig_WithMissingConfiguration_ReturnsDefaultConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act
        var config = loader.GetConfig();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(1, config.Version);
        Assert.Equal(0.20, config.Weights.Availability);
        Assert.Equal(0.35, config.Weights.Rating);
        Assert.Equal(0.45, config.Weights.Distance);
    }

    [Fact]
    public void GetConfig_WithInvalidWeight_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "1" },
                { "ScoringWeights:Weights:Availability", "1.5" }, // Invalid: > 1.0
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => loader.GetConfig());
    }

    [Fact]
    public void GetConfig_WithNegativeWeight_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "1" },
                { "ScoringWeights:Weights:Availability", "-0.1" }, // Invalid: < 0
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => loader.GetConfig());
    }

    [Fact]
    public void GetConfig_WithInvalidVersion_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "0" }, // Invalid: < 1
                { "ScoringWeights:Weights:Availability", "0.20" },
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => loader.GetConfig());
    }

    [Fact]
    public void GetConfig_WithInvalidRotationBoost_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "1" },
                { "ScoringWeights:Weights:Availability", "0.20" },
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" },
                { "ScoringWeights:Rotation:Enabled", "true" },
                { "ScoringWeights:Rotation:Boost", "25.0" }, // Invalid: > 20.0
                { "ScoringWeights:Rotation:UnderUtilizationThreshold", "0.20" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => loader.GetConfig());
    }

    [Fact]
    public void GetConfig_CachesConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "1" },
                { "ScoringWeights:Weights:Availability", "0.20" },
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act
        var config1 = loader.GetConfig();
        var config2 = loader.GetConfig();

        // Assert
        Assert.Same(config1, config2); // Should return cached instance
    }

    [Fact]
    public void GetConfigVersion_ReturnsVersion()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScoringWeights:Version", "5" },
                { "ScoringWeights:Weights:Availability", "0.20" },
                { "ScoringWeights:Weights:Rating", "0.35" },
                { "ScoringWeights:Weights:Distance", "0.45" }
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<ScoringWeightsConfigLoader>();
        var loader = new ScoringWeightsConfigLoader(configuration, logger);

        // Act
        var version = loader.GetConfigVersion();

        // Assert
        Assert.Equal(5, version);
    }
}



using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.Configuration;

namespace SmartScheduler.Infrastructure.Configuration;

/// <summary>
/// Loads scoring weights configuration from appsettings.json.
/// For MVP, uses appsettings. In production, this can be extended to load from SSM Parameter Store or AppConfig.
/// </summary>
public class ScoringWeightsConfigLoader : IScoringWeightsConfigLoader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScoringWeightsConfigLoader> _logger;
    private ScoringWeightsConfig? _cachedConfig;

    public ScoringWeightsConfigLoader(
        IConfiguration configuration,
        ILogger<ScoringWeightsConfigLoader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ScoringWeightsConfig GetConfig()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        var config = new ScoringWeightsConfig();

        // Load from configuration section
        var section = _configuration.GetSection("ScoringWeights");
        
        if (!section.Exists())
        {
            _logger.LogWarning("ScoringWeights configuration section not found. Using default values.");
            _cachedConfig = GetDefaultConfig();
            return _cachedConfig;
        }

        // Load version
        config.Version = section.GetValue<int>("Version", 1);

        // Load weights
        var weightsSection = section.GetSection("Weights");
        config.Weights = new WeightFactors
        {
            Availability = weightsSection.GetValue<double>("Availability", 0.20),
            Rating = weightsSection.GetValue<double>("Rating", 0.35),
            Distance = weightsSection.GetValue<double>("Distance", 0.45)
        };

        // Load tie-breakers
        config.TieBreakers = section.GetSection("TieBreakers").Get<List<string>>() 
            ?? new List<string> { "earliestStart", "lowerDayUtilization", "shortestNextLeg" };

        // Load rotation config
        var rotationSection = section.GetSection("Rotation");
        config.Rotation = new RotationConfig
        {
            Enabled = rotationSection.GetValue<bool>("Enabled", true),
            Boost = rotationSection.GetValue<double>("Boost", 3.0),
            UnderUtilizationThreshold = rotationSection.GetValue<double>("UnderUtilizationThreshold", 0.20)
        };

        // Validate configuration
        ValidateConfig(config);

        _cachedConfig = config;
        _logger.LogInformation("Loaded scoring weights configuration version {Version}", config.Version);
        
        return config;
    }

    public int GetConfigVersion()
    {
        return GetConfig().Version;
    }

    private void ValidateConfig(ScoringWeightsConfig config)
    {
        var errors = new List<string>();

        // Validate weights are in valid range (0.0-1.0)
        if (config.Weights.Availability < 0.0 || config.Weights.Availability > 1.0)
        {
            errors.Add($"Availability weight must be between 0.0 and 1.0, got {config.Weights.Availability}");
        }

        if (config.Weights.Rating < 0.0 || config.Weights.Rating > 1.0)
        {
            errors.Add($"Rating weight must be between 0.0 and 1.0, got {config.Weights.Rating}");
        }

        if (config.Weights.Distance < 0.0 || config.Weights.Distance > 1.0)
        {
            errors.Add($"Distance weight must be between 0.0 and 1.0, got {config.Weights.Distance}");
        }

        // Validate weight sum (should be close to 1.0, but allow some flexibility)
        var weightSum = config.Weights.Availability + config.Weights.Rating + config.Weights.Distance;
        if (weightSum < 0.1 || weightSum > 1.5)
        {
            errors.Add($"Sum of weights should be reasonable (0.1-1.5), got {weightSum}");
        }

        // Validate rotation config
        if (config.Rotation.Enabled)
        {
            if (config.Rotation.Boost < 0.0 || config.Rotation.Boost > 20.0)
            {
                errors.Add($"Rotation boost must be between 0.0 and 20.0, got {config.Rotation.Boost}");
            }

            if (config.Rotation.UnderUtilizationThreshold < 0.0 || config.Rotation.UnderUtilizationThreshold > 1.0)
            {
                errors.Add($"UnderUtilizationThreshold must be between 0.0 and 1.0, got {config.Rotation.UnderUtilizationThreshold}");
            }
        }

        // Validate version
        if (config.Version < 1)
        {
            errors.Add($"Version must be >= 1, got {config.Version}");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogError("Scoring weights configuration validation failed: {Errors}", errorMessage);
            throw new InvalidOperationException($"Invalid scoring weights configuration: {errorMessage}");
        }
    }

    private ScoringWeightsConfig GetDefaultConfig()
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
            TieBreakers = new List<string> { "earliestStart", "lowerDayUtilization", "shortestNextLeg" },
            Rotation = new RotationConfig
            {
                Enabled = true,
                Boost = 3.0,
                UnderUtilizationThreshold = 0.20
            }
        };
    }
}



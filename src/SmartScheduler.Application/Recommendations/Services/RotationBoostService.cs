using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.Configuration;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for calculating rotation boost for underutilized contractors.
/// Applies a small boost to contractors with utilization below the threshold.
/// </summary>
public class RotationBoostService : IRotationBoostService
{
    private readonly IScoringWeightsConfigLoader _configLoader;
    private readonly ILogger<RotationBoostService> _logger;

    public RotationBoostService(
        IScoringWeightsConfigLoader configLoader,
        ILogger<RotationBoostService> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public double? CalculateBoost(double utilization)
    {
        var config = _configLoader.GetConfig();

        // If rotation is disabled, return null
        if (!config.Rotation.Enabled)
        {
            return null;
        }

        // Validate utilization range
        if (utilization < 0.0 || utilization > 1.0)
        {
            _logger.LogWarning("Invalid utilization value: {Utilization}. Expected 0.0-1.0. Using 0.0.", utilization);
            utilization = Math.Max(0.0, Math.Min(1.0, utilization));
        }

        // Only apply boost if utilization is below threshold
        if (utilization >= config.Rotation.UnderUtilizationThreshold)
        {
            return null; // No boost for adequately utilized contractors
        }

        // Calculate boost: higher boost for lower utilization
        // Formula: boost = maxBoost * (1 - utilization/threshold)
        // This creates a linear decay from max boost at 0% utilization to 0 boost at threshold
        var utilizationRatio = utilization / config.Rotation.UnderUtilizationThreshold;
        var boostMultiplier = 1.0 - utilizationRatio;
        var boost = config.Rotation.Boost * boostMultiplier;

        // Ensure boost is in valid range (0 to configured boost amount)
        boost = Math.Max(0, Math.Min(config.Rotation.Boost, boost));

        return boost;
    }
}


namespace SmartScheduler.Application.Recommendations.Configuration;

/// <summary>
/// Service for loading and managing scoring weights configuration.
/// </summary>
public interface IScoringWeightsConfigLoader
{
    /// <summary>
    /// Gets the current scoring weights configuration.
    /// </summary>
    /// <returns>The current configuration with version.</returns>
    ScoringWeightsConfig GetConfig();

    /// <summary>
    /// Gets the configuration version currently in use.
    /// </summary>
    /// <returns>The version number.</returns>
    int GetConfigVersion();
}


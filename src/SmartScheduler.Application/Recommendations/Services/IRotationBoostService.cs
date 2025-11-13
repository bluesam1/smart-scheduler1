namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for calculating rotation boost for underutilized contractors.
/// </summary>
public interface IRotationBoostService
{
    /// <summary>
    /// Calculates rotation boost score (0-100) based on contractor utilization.
    /// </summary>
    /// <param name="utilization">Current utilization percentage (0.0-1.0)</param>
    /// <returns>Boost score (0-100) or null if boost should not be applied</returns>
    double? CalculateBoost(double utilization);
}



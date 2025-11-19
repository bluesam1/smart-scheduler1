namespace SmartScheduler.Application.DTOs;

/// <summary>
/// Breakdown of scoring factors for a recommendation.
/// </summary>
public class ScoreBreakdown
{
    /// <summary>
    /// Availability score (0-100).
    /// </summary>
    public double Availability { get; set; }

    /// <summary>
    /// Rating score (0-100).
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Distance score (0-100, normalized).
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Optional rotation boost score (0-100).
    /// </summary>
    public double? Rotation { get; set; }
}





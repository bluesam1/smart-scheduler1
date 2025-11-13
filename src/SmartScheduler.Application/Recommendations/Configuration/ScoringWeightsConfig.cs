namespace SmartScheduler.Application.Recommendations.Configuration;

/// <summary>
/// Configuration for scoring weights used in contractor ranking.
/// </summary>
public class ScoringWeightsConfig
{
    /// <summary>
    /// Configuration version number for tracking and audit purposes.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Weight factors for scoring calculation.
    /// </summary>
    public WeightFactors Weights { get; set; } = new();

    /// <summary>
    /// Tie-breaker order (applied when scores are equal).
    /// </summary>
    public List<string> TieBreakers { get; set; } = new();

    /// <summary>
    /// Rotation boost configuration for fair distribution.
    /// </summary>
    public RotationConfig Rotation { get; set; } = new();
}

/// <summary>
/// Weight factors for individual scoring components.
/// </summary>
public class WeightFactors
{
    /// <summary>
    /// Weight for availability score (0.0-1.0).
    /// </summary>
    public double Availability { get; set; }

    /// <summary>
    /// Weight for rating score (0.0-1.0).
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Weight for distance score (0.0-1.0).
    /// </summary>
    public double Distance { get; set; }
}

/// <summary>
/// Configuration for soft rotation boost.
/// </summary>
public class RotationConfig
{
    /// <summary>
    /// Whether rotation boost is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Boost amount to apply (typically 0-10 points).
    /// </summary>
    public double Boost { get; set; }

    /// <summary>
    /// Utilization threshold below which contractors are considered underutilized (0.0-1.0).
    /// </summary>
    public double UnderUtilizationThreshold { get; set; }
}


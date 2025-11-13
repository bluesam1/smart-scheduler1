namespace SmartScheduler.Application.Recommendations.DTOs;

/// <summary>
/// Response DTO for contractor recommendations.
/// </summary>
public record RecommendationResponse
{
    /// <summary>
    /// Unique request ID for this recommendation request.
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Job ID that recommendations were generated for.
    /// </summary>
    public Guid JobId { get; init; }

    /// <summary>
    /// Ranked list of contractor recommendations.
    /// </summary>
    public IReadOnlyList<RecommendationDto> Recommendations { get; init; } = Array.Empty<RecommendationDto>();

    /// <summary>
    /// Configuration version used for scoring weights.
    /// </summary>
    public int ConfigVersion { get; init; }

    /// <summary>
    /// Timestamp when recommendations were generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; }
}


namespace SmartScheduler.Application.Recommendations.DTOs;

/// <summary>
/// Request DTO for getting contractor recommendations for a job.
/// </summary>
public record RecommendationRequest
{
    /// <summary>
    /// Job ID to get recommendations for.
    /// </summary>
    public Guid JobId { get; init; }

    /// <summary>
    /// Desired date for the job (date only, no time).
    /// </summary>
    public DateOnly DesiredDate { get; init; }

    /// <summary>
    /// Optional service window override. If not provided, uses job's service window.
    /// </summary>
    public TimeWindowDto? ServiceWindow { get; init; }

    /// <summary>
    /// Maximum number of recommendations to return (default: 10, max: 50).
    /// </summary>
    public int MaxResults { get; init; } = 10;
}

/// <summary>
/// Time window DTO for recommendation requests.
/// </summary>
public record TimeWindowDto
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}


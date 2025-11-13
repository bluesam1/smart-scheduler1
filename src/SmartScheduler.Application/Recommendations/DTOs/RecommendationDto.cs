using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.DTOs;

namespace SmartScheduler.Application.Recommendations.DTOs;

/// <summary>
/// DTO for a single contractor recommendation.
/// </summary>
public record RecommendationDto
{
    /// <summary>
    /// Contractor ID.
    /// </summary>
    public Guid ContractorId { get; init; }

    /// <summary>
    /// Contractor name.
    /// </summary>
    public string ContractorName { get; init; } = string.Empty;

    /// <summary>
    /// Overall recommendation score (0-100).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Per-factor score breakdown.
    /// </summary>
    public ScoreBreakdown ScoreBreakdown { get; init; } = new();

    /// <summary>
    /// Human-readable rationale for the recommendation (≤200 chars).
    /// </summary>
    public string Rationale { get; init; } = string.Empty;

    /// <summary>
    /// Up to 3 suggested time slots for this contractor.
    /// </summary>
    public IReadOnlyList<TimeSlotDto> SuggestedSlots { get; init; } = Array.Empty<TimeSlotDto>();

    /// <summary>
    /// Distance from contractor base to job site in meters.
    /// </summary>
    public double Distance { get; init; }

    /// <summary>
    /// Estimated travel time in minutes.
    /// </summary>
    public int Eta { get; init; }
}


using SmartScheduler.Application.DTOs;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for generating human-readable rationale for recommendations.
/// </summary>
public interface IRationaleGenerator
{
    /// <summary>
    /// Generates a deterministic, human-readable rationale for a recommendation.
    /// </summary>
    /// <param name="scoreBreakdown">Score breakdown with factor scores</param>
    /// <param name="finalScore">Final weighted score</param>
    /// <returns>Rationale string (≤200 characters)</returns>
    string GenerateRationale(ScoreBreakdown scoreBreakdown, double finalScore);
}


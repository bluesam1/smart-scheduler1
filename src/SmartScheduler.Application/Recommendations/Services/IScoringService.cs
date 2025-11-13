using SmartScheduler.Application.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for calculating contractor recommendation scores.
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// Calculates the overall recommendation score for a contractor.
    /// </summary>
    /// <param name="contractorRating">Contractor rating (0-100)</param>
    /// <param name="availableSlots">Available time slots for the contractor</param>
    /// <param name="distanceMeters">Distance from contractor base to job location in meters</param>
    /// <param name="rotationBoost">Optional rotation boost score (0-100)</param>
    /// <returns>Overall score (0-100) and score breakdown</returns>
    ScoreResult CalculateScore(
        int contractorRating,
        IReadOnlyList<TimeWindow> availableSlots,
        double distanceMeters,
        double? rotationBoost = null);
}

/// <summary>
/// Result of scoring calculation including breakdown.
/// </summary>
public class ScoreResult
{
    /// <summary>
    /// Overall weighted score (0-100).
    /// </summary>
    public double FinalScore { get; set; }

    /// <summary>
    /// Per-factor score breakdown.
    /// </summary>
    public ScoreBreakdown Breakdown { get; set; } = new();
}


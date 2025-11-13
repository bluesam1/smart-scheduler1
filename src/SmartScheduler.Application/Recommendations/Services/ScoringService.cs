using Microsoft.Extensions.Logging;
using SmartScheduler.Application.DTOs;
using SmartScheduler.Application.Recommendations.Configuration;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for calculating contractor recommendation scores using weighted factors.
/// </summary>
public class ScoringService : IScoringService
{
    private readonly IScoringWeightsConfigLoader _configLoader;
    private readonly ILogger<ScoringService> _logger;

    // Constants for distance normalization
    private const double MaxDistanceMeters = 100000; // 100km - beyond this, score approaches 0
    private const double OptimalDistanceMeters = 0; // Zero distance = perfect score

    public ScoringService(
        IScoringWeightsConfigLoader configLoader,
        ILogger<ScoringService> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public ScoreResult CalculateScore(
        int contractorRating,
        IReadOnlyList<TimeWindow> availableSlots,
        double distanceMeters,
        double? rotationBoost = null)
    {
        var config = _configLoader.GetConfig();
        var breakdown = new ScoreBreakdown();

        // Calculate availability score (0-100)
        breakdown.Availability = CalculateAvailabilityScore(availableSlots);

        // Rating score is direct (0-100)
        breakdown.Rating = contractorRating;

        // Calculate distance score (0-100, normalized)
        breakdown.Distance = CalculateDistanceScore(distanceMeters);

        // Optional rotation boost
        if (rotationBoost.HasValue && config.Rotation.Enabled)
        {
            breakdown.Rotation = rotationBoost.Value;
        }

        // Calculate weighted final score
        var finalScore = CalculateWeightedScore(breakdown, config);

        // Ensure final score is in 0-100 range
        finalScore = Math.Max(0, Math.Min(100, finalScore));

        return new ScoreResult
        {
            FinalScore = finalScore,
            Breakdown = breakdown
        };
    }

    /// <summary>
    /// Calculates availability score based on number and duration of available slots.
    /// More slots and longer total duration = higher score.
    /// </summary>
    private double CalculateAvailabilityScore(IReadOnlyList<TimeWindow> availableSlots)
    {
        if (availableSlots == null || availableSlots.Count == 0)
        {
            return 0;
        }

        // Calculate total available duration in minutes
        var totalDurationMinutes = availableSlots.Sum(slot => (slot.End - slot.Start).TotalMinutes);

        // Score based on:
        // - Number of slots (more slots = more flexibility)
        // - Total duration (more time = better availability)
        // Normalize to 0-100 range

        // Ideal: 5+ slots with 8+ hours total duration = 100
        const double idealSlotCount = 5.0;
        const double idealDurationHours = 8.0;

        var slotCountScore = Math.Min(100, (availableSlots.Count / idealSlotCount) * 50);
        var durationScore = Math.Min(50, (totalDurationMinutes / (idealDurationHours * 60)) * 50);

        var availabilityScore = slotCountScore + durationScore;

        // Cap at 100
        return Math.Min(100, availabilityScore);
    }

    /// <summary>
    /// Calculates distance score. Shorter distance = higher score.
    /// </summary>
    private double CalculateDistanceScore(double distanceMeters)
    {
        if (distanceMeters < 0)
        {
            _logger.LogWarning("Negative distance provided: {Distance}. Using 0.", distanceMeters);
            distanceMeters = 0;
        }

        // Zero distance = perfect score (100)
        if (distanceMeters <= OptimalDistanceMeters)
        {
            return 100;
        }

        // Normalize distance to 0-100 score using exponential decay
        // Formula: score = 100 * e^(-distance / scale)
        // Scale chosen so that 10km ≈ 50 score, 50km ≈ 10 score
        const double scale = 15000; // meters

        var score = 100 * Math.Exp(-distanceMeters / scale);

        // Ensure minimum score of 0 for very long distances
        if (distanceMeters > MaxDistanceMeters)
        {
            score = 0;
        }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Calculates weighted final score from factor scores.
    /// </summary>
    private double CalculateWeightedScore(ScoreBreakdown breakdown, ScoringWeightsConfig config)
    {
        var weights = config.Weights;

        // Calculate weighted sum
        var weightedSum = 
            (breakdown.Availability * weights.Availability) +
            (breakdown.Rating * weights.Rating) +
            (breakdown.Distance * weights.Distance);

        // Apply rotation boost if enabled and present
        if (breakdown.Rotation.HasValue && config.Rotation.Enabled)
        {
            // Rotation boost is a small additive boost (not weighted)
            weightedSum += config.Rotation.Boost;
        }

        return weightedSum;
    }
}


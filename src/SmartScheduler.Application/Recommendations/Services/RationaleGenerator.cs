using SmartScheduler.Application.DTOs;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for generating human-readable rationale for recommendations.
/// Uses template-based approach for deterministic, consistent output.
/// </summary>
public class RationaleGenerator : IRationaleGenerator
{
    private const int MaxLength = 200;

    public string GenerateRationale(ScoreBreakdown scoreBreakdown, double finalScore)
    {
        // Determine primary strength (highest factor score)
        var factors = new[]
        {
            ("availability", scoreBreakdown.Availability),
            ("rating", scoreBreakdown.Rating),
            ("distance", scoreBreakdown.Distance)
        };

        var primaryFactor = factors.OrderByDescending(f => f.Item2).First();

        // Select template based on primary strength
        string rationale;
        if (primaryFactor.Item2 >= 80)
        {
            // High score in primary factor
            rationale = GenerateHighScoreRationale(primaryFactor.Item1, primaryFactor.Item2, scoreBreakdown);
        }
        else if (primaryFactor.Item2 >= 60)
        {
            // Good score in primary factor
            rationale = GenerateGoodScoreRationale(primaryFactor.Item1, primaryFactor.Item2, scoreBreakdown);
        }
        else
        {
            // Balanced or lower scores
            rationale = GenerateBalancedRationale(scoreBreakdown, finalScore);
        }

        // Ensure rationale doesn't exceed max length
        if (rationale.Length > MaxLength)
        {
            rationale = rationale.Substring(0, MaxLength - 3) + "...";
        }

        return rationale;
    }

    private string GenerateHighScoreRationale(string primaryFactor, double score, ScoreBreakdown breakdown)
    {
        return primaryFactor switch
        {
            "availability" => $"Excellent availability ({score:F0}%) with {GetRatingDescription(breakdown.Rating)} rating and {GetDistanceDescription(breakdown.Distance)} distance.",
            "rating" => $"Top-rated contractor ({score:F0}%) with {GetAvailabilityDescription(breakdown.Availability)} availability and {GetDistanceDescription(breakdown.Distance)} distance.",
            "distance" => $"Very close location ({GetDistanceDescription(breakdown.Distance)}) with {GetRatingDescription(breakdown.Rating)} rating and {GetAvailabilityDescription(breakdown.Availability)} availability.",
            _ => GenerateBalancedRationale(breakdown, breakdown.Availability + breakdown.Rating + breakdown.Distance)
        };
    }

    private string GenerateGoodScoreRationale(string primaryFactor, double score, ScoreBreakdown breakdown)
    {
        return primaryFactor switch
        {
            "availability" => $"Good availability ({score:F0}%) and {GetRatingDescription(breakdown.Rating)} rating. {GetDistanceDescription(breakdown.Distance)} distance.",
            "rating" => $"{GetRatingDescription(breakdown.Rating)} contractor ({score:F0}%) with {GetAvailabilityDescription(breakdown.Availability)} availability.",
            "distance" => $"Close location ({GetDistanceDescription(breakdown.Distance)}) with {GetRatingDescription(breakdown.Rating)} rating.",
            _ => GenerateBalancedRationale(breakdown, breakdown.Availability + breakdown.Rating + breakdown.Distance)
        };
    }

    private string GenerateBalancedRationale(ScoreBreakdown breakdown, double finalScore)
    {
        var parts = new List<string>();

        if (breakdown.Availability >= 50)
            parts.Add($"{GetAvailabilityDescription(breakdown.Availability)} availability");
        
        if (breakdown.Rating >= 50)
            parts.Add($"{GetRatingDescription(breakdown.Rating)} rating");

        if (breakdown.Distance >= 50)
            parts.Add($"{GetDistanceDescription(breakdown.Distance)} distance");

        if (parts.Count == 0)
            return $"Balanced candidate with overall score {finalScore:F0}%.";

        return $"Balanced candidate: {string.Join(", ", parts)}. Overall score {finalScore:F0}%.";
    }

    private string GetAvailabilityDescription(double score)
    {
        return score switch
        {
            >= 80 => "excellent",
            >= 60 => "good",
            >= 40 => "moderate",
            _ => "limited"
        };
    }

    private string GetRatingDescription(double score)
    {
        return score switch
        {
            >= 90 => "excellent",
            >= 75 => "strong",
            >= 60 => "good",
            >= 50 => "average",
            _ => "below average"
        };
    }

    private string GetDistanceDescription(double score)
    {
        return score switch
        {
            >= 80 => "very close",
            >= 60 => "close",
            >= 40 => "moderate",
            _ => "distant"
        };
    }
}





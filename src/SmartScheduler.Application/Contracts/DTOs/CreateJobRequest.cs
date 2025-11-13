namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Request DTO for creating a new job.
/// </summary>
public record CreateJobRequest
{
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Duration { get; init; } // Duration in minutes
    public GeoLocationDto Location { get; init; } = null!;
    public string? PlaceId { get; init; } // Google Places place_id for address validation
    public TimeWindowDto ServiceWindow { get; init; } = null!;
    public string Priority { get; init; } = "Normal"; // "Normal", "High", "Rush"
    public IReadOnlyList<string> RequiredSkills { get; init; } = Array.Empty<string>();
    public string? AccessNotes { get; init; }
    public IReadOnlyList<string>? Tools { get; init; }
    public DateTime DesiredDate { get; init; }
}


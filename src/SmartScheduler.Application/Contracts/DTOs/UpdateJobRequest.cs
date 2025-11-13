namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Request DTO for updating an existing job.
/// </summary>
public record UpdateJobRequest
{
    public string? Type { get; init; }
    public string? Description { get; init; }
    public int? Duration { get; init; }
    public GeoLocationDto? Location { get; init; }
    public string? PlaceId { get; init; } // Google Places place_id for address validation
    public TimeWindowDto? ServiceWindow { get; init; }
    public string? Priority { get; init; }
    public IReadOnlyList<string>? RequiredSkills { get; init; }
    public string? AccessNotes { get; init; }
    public IReadOnlyList<string>? Tools { get; init; }
    public DateTime? DesiredDate { get; init; }
}


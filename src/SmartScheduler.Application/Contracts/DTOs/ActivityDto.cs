namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for activity feed items.
/// </summary>
public record ActivityDto
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // "assignment", "completion", "cancellation", "contractor_added", "job_created"
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}





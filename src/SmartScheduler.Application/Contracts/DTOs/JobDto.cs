namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for job data.
/// </summary>
public record JobDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Duration { get; init; } // Duration in minutes
    public GeoLocationDto Location { get; init; } = null!;
    public string Timezone { get; init; } = string.Empty;
    public IReadOnlyList<string> RequiredSkills { get; init; } = Array.Empty<string>();
    public TimeWindowDto ServiceWindow { get; init; } = null!;
    public string Priority { get; init; } = string.Empty; // "Normal", "High", "Rush"
    public string Status { get; init; } = string.Empty; // "Created", "Assigned", "InProgress", "Completed", "Cancelled"
    public string AssignmentStatus { get; init; } = string.Empty; // "Unassigned", "PartiallyAssigned", "Assigned"
    public IReadOnlyList<ContractorAssignmentDto> AssignedContractors { get; init; } = Array.Empty<ContractorAssignmentDto>();
    public string? AccessNotes { get; init; }
    public IReadOnlyList<string>? Tools { get; init; }
    public DateTime DesiredDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record TimeWindowDto
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}

public record ContractorAssignmentDto
{
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
}


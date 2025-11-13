namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for assignment data.
/// </summary>
public record AssignmentDto
{
    public Guid Id { get; init; }
    public Guid JobId { get; init; }
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public string Source { get; init; } = string.Empty; // "Auto" or "Manual"
    public Guid? AuditId { get; init; }
    public string Status { get; init; } = string.Empty; // "Pending", "Confirmed", "InProgress", "Completed", "Cancelled"
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Request DTO for assigning a job to a contractor.
/// </summary>
public record AssignJobRequest
{
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public Guid? AuditId { get; init; }
    public string Source { get; init; } = "Auto"; // "Auto" or "Manual"
}



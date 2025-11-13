using SmartScheduler.Domain.Contracts.Events;

namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a job is cancelled.
/// </summary>
public record JobCancelled : DomainEvent
{
    public Guid JobId { get; init; }
    public string Reason { get; init; } = string.Empty;

    public JobCancelled(Guid jobId, string reason)
    {
        JobId = jobId;
        Reason = reason;
    }
}



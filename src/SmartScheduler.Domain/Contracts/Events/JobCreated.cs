using SmartScheduler.Domain.Contracts.Events;

namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a job is created.
/// </summary>
public record JobCreated : DomainEvent
{
    public Guid JobId { get; init; }
    public string JobType { get; init; } = string.Empty;
    public string LocationAddress { get; init; } = string.Empty;

    public JobCreated(Guid jobId, string jobType, string locationAddress)
    {
        JobId = jobId;
        JobType = jobType;
        LocationAddress = locationAddress;
    }
}





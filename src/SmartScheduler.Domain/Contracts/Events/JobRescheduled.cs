using SmartScheduler.Domain.Contracts.Events;

namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a job is rescheduled.
/// </summary>
public record JobRescheduled : DomainEvent
{
    public Guid JobId { get; init; }
    public DateTime PreviousServiceWindowStart { get; init; }
    public DateTime PreviousServiceWindowEnd { get; init; }
    public DateTime NewServiceWindowStart { get; init; }
    public DateTime NewServiceWindowEnd { get; init; }

    public JobRescheduled(
        Guid jobId,
        DateTime previousServiceWindowStart,
        DateTime previousServiceWindowEnd,
        DateTime newServiceWindowStart,
        DateTime newServiceWindowEnd)
    {
        JobId = jobId;
        PreviousServiceWindowStart = previousServiceWindowStart;
        PreviousServiceWindowEnd = previousServiceWindowEnd;
        NewServiceWindowStart = newServiceWindowStart;
        NewServiceWindowEnd = newServiceWindowEnd;
    }
}


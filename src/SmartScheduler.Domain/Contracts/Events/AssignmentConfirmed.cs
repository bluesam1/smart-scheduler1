namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when an assignment is confirmed.
/// </summary>
public record AssignmentConfirmed : DomainEvent
{
    public Guid AssignmentId { get; init; }
    public Guid JobId { get; init; }
    public Guid ContractorId { get; init; }

    public AssignmentConfirmed(Guid assignmentId, Guid jobId, Guid contractorId)
    {
        AssignmentId = assignmentId;
        JobId = jobId;
        ContractorId = contractorId;
    }
}



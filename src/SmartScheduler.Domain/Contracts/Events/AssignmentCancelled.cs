namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when an assignment is cancelled.
/// </summary>
public record AssignmentCancelled : DomainEvent
{
    public Guid AssignmentId { get; init; }
    public Guid JobId { get; init; }
    public Guid ContractorId { get; init; }
    public string Reason { get; init; }

    public AssignmentCancelled(Guid assignmentId, Guid jobId, Guid contractorId, string reason)
    {
        AssignmentId = assignmentId;
        JobId = jobId;
        ContractorId = contractorId;
        Reason = reason;
    }
}





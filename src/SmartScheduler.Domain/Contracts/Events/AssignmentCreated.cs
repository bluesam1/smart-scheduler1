namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when an assignment is created.
/// </summary>
public record AssignmentCreated : DomainEvent
{
    public Guid AssignmentId { get; init; }
    public Guid JobId { get; init; }
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public AssignmentCreated(Guid assignmentId, Guid jobId, Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        AssignmentId = assignmentId;
        JobId = jobId;
        ContractorId = contractorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}


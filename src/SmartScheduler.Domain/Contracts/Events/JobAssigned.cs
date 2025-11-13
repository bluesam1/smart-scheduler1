using SmartScheduler.Domain.Contracts.Events;

namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a job is assigned to a contractor.
/// </summary>
public record JobAssigned : DomainEvent
{
    public Guid JobId { get; init; }
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public JobAssigned(Guid jobId, Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        JobId = jobId;
        ContractorId = contractorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}



namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a contractor is updated.
/// </summary>
public record ContractorUpdated : DomainEvent
{
    public Guid ContractorId { get; init; }
    public string Name { get; init; }

    public ContractorUpdated(Guid contractorId, string name)
    {
        ContractorId = contractorId;
        Name = name;
    }
}



namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a contractor is created.
/// </summary>
public record ContractorCreated : DomainEvent
{
    public Guid ContractorId { get; init; }
    public string Name { get; init; }

    public ContractorCreated(Guid contractorId, string name)
    {
        ContractorId = contractorId;
        Name = name;
    }
}


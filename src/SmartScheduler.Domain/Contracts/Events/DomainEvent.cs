namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record DomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}



namespace SmartScheduler.Domain.Contracts.Events;

/// <summary>
/// Domain event raised when a contractor's rating is updated.
/// </summary>
public record ContractorRated : DomainEvent
{
    public Guid ContractorId { get; init; }
    public int PreviousRating { get; init; }
    public int NewRating { get; init; }

    public ContractorRated(Guid contractorId, int previousRating, int newRating)
    {
        ContractorId = contractorId;
        PreviousRating = previousRating;
        NewRating = newRating;
    }
}


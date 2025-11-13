using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for applying tie-breaker logic when contractors have equal scores.
/// </summary>
public interface ITieBreakerService
{
    /// <summary>
    /// Applies tie-breaker logic to order contractors with equal scores.
    /// </summary>
    /// <param name="candidates">Candidates with equal scores to order</param>
    /// <param name="availableSlots">Available slots per contractor (contractorId -> slots)</param>
    /// <param name="sameDayUtilization">Same-day utilization per contractor (contractorId -> utilization 0-1)</param>
    /// <param name="nextLegTravelMinutes">Next-leg travel time per contractor (contractorId -> minutes)</param>
    /// <returns>Ordered list of contractor IDs (best first)</returns>
    IReadOnlyList<Guid> ApplyTieBreakers(
        IReadOnlyList<Guid> candidates,
        IReadOnlyDictionary<Guid, IReadOnlyList<TimeWindow>> availableSlots,
        IReadOnlyDictionary<Guid, double> sameDayUtilization,
        IReadOnlyDictionary<Guid, int?> nextLegTravelMinutes);
}



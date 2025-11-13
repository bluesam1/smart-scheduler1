using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Recommendations.Services;

/// <summary>
/// Service for applying tie-breaker logic when contractors have equal scores.
/// Order: (1) earliest feasible start, (2) lower same-day utilization, (3) shortest next-leg travel.
/// </summary>
public class TieBreakerService : ITieBreakerService
{
    private readonly ILogger<TieBreakerService> _logger;

    public TieBreakerService(ILogger<TieBreakerService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<Guid> ApplyTieBreakers(
        IReadOnlyList<Guid> candidates,
        IReadOnlyDictionary<Guid, IReadOnlyList<TimeWindow>> availableSlots,
        IReadOnlyDictionary<Guid, double> sameDayUtilization,
        IReadOnlyDictionary<Guid, int?> nextLegTravelMinutes)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        if (candidates.Count == 1)
        {
            return candidates;
        }

        // Create a list of candidates with tie-breaker data
        var candidateData = candidates.Select(id => new
        {
            Id = id,
            EarliestStart = GetEarliestStart(availableSlots.GetValueOrDefault(id)),
            Utilization = sameDayUtilization.GetValueOrDefault(id, 0.0),
            NextLegTravel = nextLegTravelMinutes.GetValueOrDefault(id)
        }).ToList();

        // Apply tie-breakers in order
        var ordered = candidateData
            .OrderBy(c => c.EarliestStart) // (1) Earliest start first
            .ThenBy(c => c.Utilization)    // (2) Lower utilization first
            .ThenBy(c => c.NextLegTravel ?? int.MaxValue) // (3) Shorter travel first (null = max)
            .Select(c => c.Id)
            .ToList();

        return ordered;
    }

    /// <summary>
    /// Gets the earliest feasible start time from available slots.
    /// Returns DateTime.MaxValue if no slots available.
    /// </summary>
    private DateTime GetEarliestStart(IReadOnlyList<TimeWindow>? slots)
    {
        if (slots == null || slots.Count == 0)
        {
            return DateTime.MaxValue;
        }

        return slots.Min(slot => slot.Start);
    }
}



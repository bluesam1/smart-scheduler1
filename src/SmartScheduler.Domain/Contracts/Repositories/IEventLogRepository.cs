using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for EventLog entities.
/// </summary>
public interface IEventLogRepository
{
    Task<IReadOnlyList<EventLog>> GetRecentAsync(
        int limit = 20,
        IReadOnlyList<string>? eventTypes = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(EventLog eventLog, CancellationToken cancellationToken = default);
}



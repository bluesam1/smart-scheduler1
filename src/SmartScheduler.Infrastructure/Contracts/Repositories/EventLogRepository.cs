using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IEventLogRepository.
/// </summary>
public class EventLogRepository : IEventLogRepository
{
    private readonly SmartSchedulerDbContext _context;

    public EventLogRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EventLog>> GetRecentAsync(
        int limit = 20,
        IReadOnlyList<string>? eventTypes = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<EventLog>().AsQueryable();

        if (eventTypes != null && eventTypes.Count > 0)
        {
            query = query.Where(e => eventTypes.Contains(e.EventType));
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EventLog eventLog, CancellationToken cancellationToken = default)
    {
        await _context.Set<EventLog>().AddAsync(eventLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}





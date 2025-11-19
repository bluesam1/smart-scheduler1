using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IJobRepository.
/// </summary>
public class JobRepository : IJobRepository
{
    private readonly SmartSchedulerDbContext _context;

    public JobRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Job>()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Job>()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Job>()
            .Where(j => j.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Job>()
            .Where(j => j.Priority == priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await _context.Set<Job>().AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        _context.Set<Job>().Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Job job, CancellationToken cancellationToken = default)
    {
        _context.Set<Job>().Remove(job);
        await _context.SaveChangesAsync(cancellationToken);
    }
}





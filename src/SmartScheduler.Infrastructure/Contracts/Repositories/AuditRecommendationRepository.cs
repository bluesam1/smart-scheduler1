using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IAuditRecommendationRepository.
/// </summary>
public class AuditRecommendationRepository : IAuditRecommendationRepository
{
    private readonly SmartSchedulerDbContext _context;

    public AuditRecommendationRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditRecommendation auditRecommendation, CancellationToken cancellationToken = default)
    {
        await _context.Set<AuditRecommendation>().AddAsync(auditRecommendation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuditRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditRecommendation>()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecommendation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditRecommendation>()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecommendation>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditRecommendation>()
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}


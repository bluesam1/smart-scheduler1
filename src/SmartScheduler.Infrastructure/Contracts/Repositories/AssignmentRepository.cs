using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IAssignmentRepository.
/// </summary>
public class AssignmentRepository : IAssignmentRepository
{
    private readonly SmartSchedulerDbContext _context;

    public AssignmentRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Assignment>()
            .Include(a => a.Job)
            .Include(a => a.Contractor)
            .Include(a => a.Audit)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Assignment>()
            .Include(a => a.Contractor)
            .Where(a => a.JobId == jobId)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetByContractorIdAsync(Guid contractorId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Assignment>()
            .Include(a => a.Job)
            .Where(a => a.ContractorId == contractorId)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetByContractorIdAndTimeRangeAsync(
        Guid contractorId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Assignment>()
            .Include(a => a.Job)
            .Where(a => a.ContractorId == contractorId &&
                       a.Status != AssignmentEntityStatus.Cancelled &&
                       ((a.StartUtc >= startUtc && a.StartUtc < endUtc) ||
                        (a.EndUtc > startUtc && a.EndUtc <= endUtc) ||
                        (a.StartUtc <= startUtc && a.EndUtc >= endUtc)))
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetActiveAssignmentsByContractorIdsAsync(
        IReadOnlyList<Guid> contractorIds,
        CancellationToken cancellationToken = default)
    {
        if (contractorIds == null || contractorIds.Count == 0)
            return Array.Empty<Assignment>();

        return await _context.Set<Assignment>()
            .Where(a => contractorIds.Contains(a.ContractorId) &&
                       a.Status != AssignmentEntityStatus.Cancelled &&
                       a.Status != AssignmentEntityStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.Set<Assignment>().AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        _context.Set<Assignment>().Update(assignment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}


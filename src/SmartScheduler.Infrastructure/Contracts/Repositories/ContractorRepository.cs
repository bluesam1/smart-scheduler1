using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IContractorRepository.
/// </summary>
public class ContractorRepository : IContractorRepository
{
    private readonly SmartSchedulerDbContext _context;

    public ContractorRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Contractor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contractor>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contractor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contractor>()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contractor>> GetBySkillsAsync(
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default)
    {
        // Normalize skills for comparison
        var normalizedSkills = skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToLowerInvariant())
            .ToList();

        if (normalizedSkills.Count == 0)
            return Array.Empty<Contractor>();

        // EF Core doesn't support direct array contains for value objects,
        // so we'll need to use a different approach
        // For now, we'll load all and filter in memory (can be optimized later)
        var allContractors = await GetAllAsync(cancellationToken);
        
        return allContractors
            .Where(c => normalizedSkills.All(skill => 
                c.Skills.Any(contractorSkill => contractorSkill == skill)))
            .ToList();
    }

    public async Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default)
    {
        await _context.Set<Contractor>().AddAsync(contractor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Contractor contractor, CancellationToken cancellationToken = default)
    {
        _context.Set<Contractor>().Update(contractor);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contractor = await GetByIdAsync(id, cancellationToken);
        if (contractor != null)
        {
            _context.Set<Contractor>().Remove(contractor);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}



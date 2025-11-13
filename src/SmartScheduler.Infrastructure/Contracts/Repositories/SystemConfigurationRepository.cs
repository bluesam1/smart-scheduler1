using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of ISystemConfigurationRepository.
/// </summary>
public class SystemConfigurationRepository : ISystemConfigurationRepository
{
    private readonly SmartSchedulerDbContext _context;

    public SystemConfigurationRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<SystemConfiguration?> GetByTypeAsync(ConfigurationType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Type == type, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SystemConfiguration>()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _context.Set<SystemConfiguration>().AddAsync(configuration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _context.Set<SystemConfiguration>().Update(configuration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var configuration = await _context.Set<SystemConfiguration>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (configuration != null)
        {
            _context.Set<SystemConfiguration>().Remove(configuration);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}


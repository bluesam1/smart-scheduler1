using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Infrastructure.Data;

namespace SmartScheduler.Infrastructure.Contracts.Repositories;

/// <summary>
/// EF Core implementation of IWeightsConfigRepository.
/// </summary>
public class WeightsConfigRepository : IWeightsConfigRepository
{
    private readonly SmartSchedulerDbContext _context;

    public WeightsConfigRepository(SmartSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<WeightsConfig?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WeightsConfig>()
            .FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
    }

    public async Task<WeightsConfig?> GetByVersionAsync(int version, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WeightsConfig>()
            .FirstOrDefaultAsync(c => c.Version == version, cancellationToken);
    }

    public async Task<IReadOnlyList<WeightsConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WeightsConfig>()
            .OrderByDescending(c => c.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        var latest = await _context.Set<WeightsConfig>()
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return latest?.Version ?? 0;
    }

    public async Task<int> GetNextVersionAsync(CancellationToken cancellationToken = default)
    {
        // Use SELECT FOR UPDATE to lock the row with highest version
        // This ensures atomic version incrementing and prevents race conditions
        // when multiple admins create versions simultaneously
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            // Use raw SQL with FOR UPDATE to lock the row
            // This prevents concurrent reads of the same version number
            var latest = await _context.Set<WeightsConfig>()
                .FromSqlRaw("SELECT * FROM \"WeightsConfigs\" ORDER BY \"Version\" DESC LIMIT 1 FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            var nextVersion = (latest?.Version ?? 0) + 1;

            // Commit transaction to release the lock
            await transaction.CommitAsync(cancellationToken);

            return nextVersion;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WeightsConfig> AddAsync(WeightsConfig config, CancellationToken cancellationToken = default)
    {
        // Retry logic to handle potential unique constraint violations
        // This can occur if two requests get the same version number simultaneously
        // (though GetNextVersionAsync with FOR UPDATE should prevent this in most cases)
        int maxRetries = 3;
        int retryCount = 0;
        WeightsConfig currentConfig = config;

        while (retryCount < maxRetries)
        {
            try
            {
                // Detach any existing entity from change tracker
                var existingEntry = _context.Entry(currentConfig);
                if (existingEntry.State != EntityState.Detached)
                {
                    existingEntry.State = EntityState.Detached;
                }

                await _context.Set<WeightsConfig>().AddAsync(currentConfig, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                return currentConfig; // Return the entity that was actually saved
            }
            catch (DbUpdateException ex) when (retryCount < maxRetries - 1 && IsUniqueConstraintViolation(ex))
            {
                // Unique constraint violation on Version - another request created a version with this number
                // Remove the entity from the change tracker
                var entry = _context.Entry(currentConfig);
                if (entry.State != EntityState.Detached)
                {
                    entry.State = EntityState.Detached;
                }
                
                retryCount++;
                
                // Get a fresh version number for retry
                var newVersion = await GetNextVersionAsync(cancellationToken);
                
                // Create a new entity with the fresh version (keeping same ID and other properties)
                currentConfig = new WeightsConfig(
                    id: config.Id,
                    version: newVersion,
                    configJson: config.ConfigJson,
                    changeNotes: config.ChangeNotes,
                    createdBy: config.CreatedBy,
                    isActive: config.IsActive);
                
                await Task.Delay(50 * retryCount, cancellationToken); // Brief delay before retry
            }
        }

        // If we get here, all retries failed
        throw new InvalidOperationException("Failed to create weights configuration after retries due to concurrent version creation. Please try again.");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Check if this is a unique constraint violation on the Version column
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_WeightsConfigs_Version", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("23505", StringComparison.OrdinalIgnoreCase); // PostgreSQL unique violation error code
    }

    public async Task DeactivateAllAsync(CancellationToken cancellationToken = default)
    {
        var activeConfigs = await _context.Set<WeightsConfig>()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var config in activeConfigs)
        {
            config.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}


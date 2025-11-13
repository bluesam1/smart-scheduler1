using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for SystemConfiguration entity.
/// </summary>
public interface ISystemConfigurationRepository
{
    /// <summary>
    /// Gets a configuration by type.
    /// </summary>
    Task<SystemConfiguration?> GetByTypeAsync(ConfigurationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations.
    /// </summary>
    Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new configuration.
    /// </summary>
    Task AddAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing configuration.
    /// </summary>
    Task UpdateAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for WeightsConfig entity.
/// </summary>
public interface IWeightsConfigRepository
{
    /// <summary>
    /// Gets the active weights configuration.
    /// </summary>
    Task<WeightsConfig?> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration by version.
    /// </summary>
    Task<WeightsConfig?> GetByVersionAsync(int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations ordered by version descending.
    /// </summary>
    Task<IReadOnlyList<WeightsConfig>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version number.
    /// </summary>
    Task<int> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next version number atomically within a transaction.
    /// This method ensures thread-safe version incrementing.
    /// </summary>
    Task<int> GetNextVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new configuration.
    /// Returns the created entity (version may differ if retry occurred due to race condition).
    /// </summary>
    Task<WeightsConfig> AddAsync(WeightsConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all configurations (sets IsActive = false).
    /// </summary>
    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
}


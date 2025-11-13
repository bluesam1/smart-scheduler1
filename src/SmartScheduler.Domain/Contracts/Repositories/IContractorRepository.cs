using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for contractor aggregate root.
/// </summary>
public interface IContractorRepository
{
    /// <summary>
    /// Gets a contractor by ID.
    /// </summary>
    Task<Contractor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all contractors.
    /// </summary>
    Task<IReadOnlyList<Contractor>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets contractors by skills.
    /// </summary>
    Task<IReadOnlyList<Contractor>> GetBySkillsAsync(IReadOnlyList<string> skills, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new contractor.
    /// </summary>
    Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing contractor.
    /// </summary>
    Task UpdateAsync(Contractor contractor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a contractor.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


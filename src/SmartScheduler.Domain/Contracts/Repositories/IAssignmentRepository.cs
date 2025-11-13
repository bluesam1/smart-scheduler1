using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for Assignment aggregate root.
/// </summary>
public interface IAssignmentRepository
{
    /// <summary>
    /// Gets an assignment by ID.
    /// </summary>
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all assignments for a job.
    /// </summary>
    Task<IReadOnlyList<Assignment>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all assignments for a contractor.
    /// </summary>
    Task<IReadOnlyList<Assignment>> GetByContractorIdAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignments for a contractor within a time range.
    /// </summary>
    Task<IReadOnlyList<Assignment>> GetByContractorIdAndTimeRangeAsync(
        Guid contractorId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active assignments for multiple contractors (optimized batch query).
    /// </summary>
    Task<IReadOnlyList<Assignment>> GetActiveAssignmentsByContractorIdsAsync(
        IReadOnlyList<Guid> contractorIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new assignment.
    /// </summary>
    Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing assignment.
    /// </summary>
    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default);
}


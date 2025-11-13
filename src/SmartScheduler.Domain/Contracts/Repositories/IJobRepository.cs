using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for Job aggregate root.
/// </summary>
public interface IJobRepository
{
    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all jobs.
    /// </summary>
    Task<IReadOnlyList<Job>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs by status.
    /// </summary>
    Task<IReadOnlyList<Job>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs by priority.
    /// </summary>
    Task<IReadOnlyList<Job>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new job.
    /// </summary>
    Task AddAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing job.
    /// </summary>
    Task UpdateAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a job.
    /// </summary>
    Task DeleteAsync(Job job, CancellationToken cancellationToken = default);
}


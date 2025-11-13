using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Domain.Contracts.Repositories;

/// <summary>
/// Repository interface for AuditRecommendation entity.
/// </summary>
public interface IAuditRecommendationRepository
{
    /// <summary>
    /// Adds a new audit recommendation record.
    /// </summary>
    Task AddAsync(AuditRecommendation auditRecommendation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit recommendations by job ID.
    /// </summary>
    Task<IReadOnlyList<AuditRecommendation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit recommendations by date range.
    /// </summary>
    Task<IReadOnlyList<AuditRecommendation>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}


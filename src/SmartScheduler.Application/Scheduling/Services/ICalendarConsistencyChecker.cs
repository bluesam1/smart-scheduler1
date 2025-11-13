using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Application.Scheduling.Services;

/// <summary>
/// Service for checking calendar consistency after reschedule/cancel operations.
/// </summary>
public interface ICalendarConsistencyChecker
{
    /// <summary>
    /// Validates calendar consistency for a contractor after reschedule/cancel operations.
    /// Checks for overlapping assignments and invalid gaps (missing travel buffers).
    /// </summary>
    /// <param name="contractorId">Contractor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Consistency check result with any issues found</returns>
    Task<CalendarConsistencyResult> CheckConsistencyAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to automatically correct minor consistency issues.
    /// </summary>
    /// <param name="contractorId">Contractor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Correction result with details of any corrections made</returns>
    Task<CalendarConsistencyCorrectionResult> AttemptCorrectionAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of calendar consistency check.
/// </summary>
public record CalendarConsistencyResult
{
    public bool IsConsistent { get; init; }
    public IReadOnlyList<ConsistencyIssue> Issues { get; init; } = Array.Empty<ConsistencyIssue>();

    public static CalendarConsistencyResult Consistent() => new() { IsConsistent = true };
    
    public static CalendarConsistencyResult Inconsistent(IReadOnlyList<ConsistencyIssue> issues) => new()
    {
        IsConsistent = false,
        Issues = issues
    };
}

/// <summary>
/// Represents a consistency issue found in the calendar.
/// </summary>
public record ConsistencyIssue
{
    public ConsistencyIssueType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? AssignmentId1 { get; init; }
    public Guid? AssignmentId2 { get; init; }
}

/// <summary>
/// Types of consistency issues.
/// </summary>
public enum ConsistencyIssueType
{
    Overlap,
    InvalidGap
}

/// <summary>
/// Result of calendar consistency correction attempt.
/// </summary>
public record CalendarConsistencyCorrectionResult
{
    public bool CorrectionsMade { get; init; }
    public IReadOnlyList<string> CorrectionDetails { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ConsistencyIssue> RemainingIssues { get; init; } = Array.Empty<ConsistencyIssue>();

    public static CalendarConsistencyCorrectionResult NoCorrections() => new() { CorrectionsMade = false };
    
    public static CalendarConsistencyCorrectionResult WithCorrections(
        IReadOnlyList<string> details,
        IReadOnlyList<ConsistencyIssue> remainingIssues) => new()
    {
        CorrectionsMade = true,
        CorrectionDetails = details,
        RemainingIssues = remainingIssues
    };
}


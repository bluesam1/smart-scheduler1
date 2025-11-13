namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for re-validating contractor availability before assignment.
/// </summary>
public interface IAvailabilityRevalidator
{
    /// <summary>
    /// Validates that a contractor is available for the specified time slot.
    /// Checks working hours, existing assignments, and calendar exceptions.
    /// </summary>
    /// <param name="contractorId">Contractor ID</param>
    /// <param name="jobId">Job ID (for context)</param>
    /// <param name="startUtc">Assignment start time (UTC)</param>
    /// <param name="endUtc">Assignment end time (UTC)</param>
    /// <param name="jobDurationMinutes">Job duration in minutes</param>
    /// <param name="jobTimezone">Job location timezone</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with success status and error message if failed</returns>
    Task<AvailabilityValidationResult> ValidateAvailabilityAsync(
        Guid contractorId,
        Guid jobId,
        DateTime startUtc,
        DateTime endUtc,
        int jobDurationMinutes,
        string jobTimezone,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of availability validation.
/// </summary>
public record AvailabilityValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? ConflictingAssignmentId { get; init; }

    public static AvailabilityValidationResult Valid() => new() { IsValid = true };
    
    public static AvailabilityValidationResult Invalid(string errorMessage, Guid? conflictingAssignmentId = null) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage,
        ConflictingAssignmentId = conflictingAssignmentId
    };
}


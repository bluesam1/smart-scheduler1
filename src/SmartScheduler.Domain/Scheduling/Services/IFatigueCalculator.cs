using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Interface for calculating fatigue limits and break requirements.
/// </summary>
public interface IFatigueCalculator
{
    /// <summary>
    /// Calculates daily hours worked for a contractor on a specific date.
    /// </summary>
    /// <param name="existingAssignments">Existing job assignments (start/end UTC)</param>
    /// <param name="date">Date to calculate hours for (in contractor timezone)</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <returns>Total hours worked on the date (in hours)</returns>
    double CalculateDailyHours(
        IReadOnlyList<TimeWindow> existingAssignments,
        DateOnly date,
        string contractorTimezone);

    /// <summary>
    /// Calculates consecutive jobs count without a break.
    /// </summary>
    /// <param name="existingAssignments">Existing job assignments (start/end UTC), ordered by start time</param>
    /// <param name="beforeTime">Time to check before (UTC)</param>
    /// <returns>Number of consecutive jobs without a break</returns>
    int CalculateConsecutiveJobsCount(
        IReadOnlyList<TimeWindow> existingAssignments,
        DateTime beforeTime);

    /// <summary>
    /// Checks if a slot is feasible considering fatigue limits.
    /// </summary>
    /// <param name="proposedSlot">Proposed time slot (start/end UTC)</param>
    /// <param name="existingAssignments">Existing job assignments (start/end UTC)</param>
    /// <param name="jobDurationMinutes">Duration of the proposed job in minutes</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <param name="isRushJob">Whether this is a rush job</param>
    /// <param name="breakMinutes">Required break minutes (default 15)</param>
    /// <returns>Feasibility result with reason if not feasible</returns>
    FatigueFeasibilityResult CheckFeasibility(
        TimeWindow proposedSlot,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        bool isRushJob = false,
        int breakMinutes = 15);
}

/// <summary>
/// Result of fatigue feasibility check.
/// </summary>
public class FatigueFeasibilityResult
{
    public bool IsFeasible { get; init; }
    public string? Reason { get; init; }
    public int? RequiredBreakMinutes { get; init; }
}



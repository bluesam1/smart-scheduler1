using SmartScheduler.Domain.Contracts.ValueObjects;
using TimeZoneConverter;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Calculates fatigue limits and break requirements for contractors.
/// Enforces: 8h target, 10h soft cap, 12h hard stop, max 4 consecutive jobs without 15m break.
/// </summary>
public class FatigueCalculator : IFatigueCalculator
{
    private const double TargetDailyHours = 8.0;
    private const double SoftCapDailyHours = 10.0;
    private const double HardStopDailyHours = 12.0;
    private const int MaxConsecutiveJobsWithoutBreak = 4;
    private const int DefaultBreakMinutes = 15;

    /// <inheritdoc />
    public double CalculateDailyHours(
        IReadOnlyList<TimeWindow> existingAssignments,
        DateOnly date,
        string contractorTimezone)
    {
        if (existingAssignments == null || existingAssignments.Count == 0)
            return 0.0;

        var contractorTz = GetTimeZone(contractorTimezone);
        var dateStart = date.ToDateTime(TimeOnly.MinValue);
        var dateEnd = date.ToDateTime(TimeOnly.MaxValue);

        // Convert date boundaries to UTC
        var dateStartUtc = TimeZoneInfo.ConvertTimeToUtc(dateStart, contractorTz);
        var dateEndUtc = TimeZoneInfo.ConvertTimeToUtc(dateEnd, contractorTz);

        // Calculate total hours worked on this date
        var totalMinutes = 0.0;
        foreach (var assignment in existingAssignments)
        {
            // Find overlap with the date
            var overlapStart = assignment.Start > dateStartUtc ? assignment.Start : dateStartUtc;
            var overlapEnd = assignment.End < dateEndUtc ? assignment.End : dateEndUtc;

            if (overlapStart < overlapEnd)
            {
                totalMinutes += (overlapEnd - overlapStart).TotalMinutes;
            }
        }

        return totalMinutes / 60.0; // Convert to hours
    }

    /// <inheritdoc />
    public int CalculateConsecutiveJobsCount(
        IReadOnlyList<TimeWindow> existingAssignments,
        DateTime beforeTime)
    {
        if (existingAssignments == null || existingAssignments.Count == 0)
            return 0;

        // Get assignments before the specified time, ordered by start time (oldest first)
        var assignmentsBefore = existingAssignments
            .Where(a => a.End <= beforeTime)
            .OrderBy(a => a.Start)
            .ToList();

        if (assignmentsBefore.Count == 0)
            return 0;

        // Count consecutive jobs without a break (break = gap > 15 minutes)
        // Start from the most recent job and work backwards
        var consecutiveCount = 1; // Start with the most recent assignment
        var mostRecentEnd = assignmentsBefore[assignmentsBefore.Count - 1].End;

        // Work backwards from the most recent job
        for (int i = assignmentsBefore.Count - 2; i >= 0; i--)
        {
            // Check gap between this job's end and the next job's start
            var gap = (assignmentsBefore[i + 1].Start - assignmentsBefore[i].End).TotalMinutes;
            if (gap <= DefaultBreakMinutes)
            {
                // No significant break, count as consecutive
                consecutiveCount++;
            }
            else
            {
                // Found a break, stop counting
                break;
            }
        }

        return consecutiveCount;
    }

    /// <inheritdoc />
    public FatigueFeasibilityResult CheckFeasibility(
        TimeWindow proposedSlot,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        bool isRushJob = false,
        int breakMinutes = 15)
    {
        if (jobDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(jobDurationMinutes), "Job duration must be positive.");

        var contractorTz = GetTimeZone(contractorTimezone);

        // Convert proposed slot start to contractor timezone to get the date
        var slotStartContractor = TimeZoneInfo.ConvertTimeFromUtc(proposedSlot.Start, contractorTz);
        var slotDate = DateOnly.FromDateTime(slotStartContractor);

        // Calculate daily hours including the proposed job
        var currentDailyHours = CalculateDailyHours(existingAssignments, slotDate, contractorTimezone);
        var jobHours = jobDurationMinutes / 60.0;
        var totalDailyHours = currentDailyHours + jobHours;

        // Check hard stop (always enforced, even for rush jobs)
        if (totalDailyHours > HardStopDailyHours)
        {
            return new FatigueFeasibilityResult
            {
                IsFeasible = false,
                Reason = $"Would exceed hard stop of {HardStopDailyHours} hours (would be {totalDailyHours:F1} hours)"
            };
        }

        // Check soft cap (can be bypassed for rush jobs)
        if (totalDailyHours > SoftCapDailyHours && !isRushJob)
        {
            return new FatigueFeasibilityResult
            {
                IsFeasible = false,
                Reason = $"Would exceed soft cap of {SoftCapDailyHours} hours (would be {totalDailyHours:F1} hours). Rush job required."
            };
        }

        // Check consecutive jobs limit
        var consecutiveJobs = CalculateConsecutiveJobsCount(existingAssignments, proposedSlot.Start);
        
        // Check if there's a break before the proposed slot
        var lastAssignment = existingAssignments
            .Where(a => a.End <= proposedSlot.Start)
            .OrderByDescending(a => a.End)
            .FirstOrDefault();

        if (lastAssignment != null)
        {
            var gap = (proposedSlot.Start - lastAssignment.End).TotalMinutes;
            
            // If we already have max consecutive jobs and no sufficient break, block it
            if (consecutiveJobs >= MaxConsecutiveJobsWithoutBreak && gap < breakMinutes)
            {
                var requiredBreak = breakMinutes - (int)gap;
                return new FatigueFeasibilityResult
                {
                    IsFeasible = false,
                    Reason = $"Would exceed {MaxConsecutiveJobsWithoutBreak} consecutive jobs without break",
                    RequiredBreakMinutes = requiredBreak
                };
            }
        }
        else if (consecutiveJobs >= MaxConsecutiveJobsWithoutBreak)
        {
            // No previous assignment but already at max consecutive jobs
            // This shouldn't happen, but handle it
            return new FatigueFeasibilityResult
            {
                IsFeasible = false,
                Reason = $"Would exceed {MaxConsecutiveJobsWithoutBreak} consecutive jobs without break"
            };
        }

        // All checks passed
        return new FatigueFeasibilityResult
        {
            IsFeasible = true
        };
    }

    /// <summary>
    /// Gets TimeZoneInfo from IANA timezone identifier.
    /// </summary>
    private TimeZoneInfo GetTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        try
        {
            if (TZConvert.TryGetTimeZoneInfo(timezone, out var tz))
            {
                return tz;
            }

            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid timezone: {timezone}", nameof(timezone));
        }
    }
}


using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Interface for generating feasible time slots for contractors.
/// </summary>
public interface ISlotGenerator
{
    /// <summary>
    /// Generates up to 3 feasible slots for a contractor (earliest, lowest-travel, highest-confidence).
    /// </summary>
    /// <param name="workingHours">Contractor's weekly working hours</param>
    /// <param name="serviceWindow">Requested service window (start and end UTC)</param>
    /// <param name="existingAssignments">Existing job assignments for the contractor (start/end UTC)</param>
    /// <param name="jobDurationMinutes">Duration of the job in minutes</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <param name="jobTimezone">Job location timezone (IANA identifier)</param>
    /// <param name="calendar">Optional contractor calendar with exceptions</param>
    /// <param name="baseToJobEtaMinutes">ETA from contractor base to job location (for earliest/lowest-travel)</param>
    /// <param name="previousJobToJobEtaMinutes">ETA from previous job to this job (for lowest-travel, null if first job)</param>
    /// <param name="contractorRating">Contractor rating (0-100) for confidence calculation</param>
    /// <param name="isRushJob">Whether this is a rush job (affects fatigue limits)</param>
    /// <returns>List of up to 3 feasible slots</returns>
    IReadOnlyList<GeneratedSlot> GenerateSlots(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null,
        int? baseToJobEtaMinutes = null,
        int? previousJobToJobEtaMinutes = null,
        int contractorRating = 50,
        bool isRushJob = false);
}

/// <summary>
/// Represents a generated time slot with metadata.
/// </summary>
public class GeneratedSlot
{
    public TimeWindow Window { get; init; } = null!;
    public SlotType Type { get; init; }
    public int Confidence { get; init; } // 0-100
}

/// <summary>
/// Type of generated slot.
/// </summary>
public enum SlotType
{
    Earliest,
    LowestTravel,
    HighestConfidence
}


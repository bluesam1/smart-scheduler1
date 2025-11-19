using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Interface for availability engine that calculates feasible time slots for contractors.
/// </summary>
public interface IAvailabilityEngine
{
    /// <summary>
    /// Calculates available time slots for a contractor within a given service window.
    /// </summary>
    /// <param name="workingHours">Contractor's weekly working hours</param>
    /// <param name="serviceWindow">Requested service window (start and end UTC)</param>
    /// <param name="existingAssignments">Existing job assignments for the contractor (start/end UTC)</param>
    /// <param name="jobDurationMinutes">Duration of the job in minutes</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <param name="jobTimezone">Job location timezone (IANA identifier)</param>
    /// <param name="calendar">Optional contractor calendar with exceptions</param>
    /// <returns>List of available time slots (start/end UTC)</returns>
    IReadOnlyList<TimeWindow> CalculateAvailableSlots(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null);
}





using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Interface for calculating available time based on contractor working hours.
/// </summary>
public interface IWorkingHoursCalculator
{
    /// <summary>
    /// Calculates available time windows from working hours within a service window.
    /// </summary>
    /// <param name="workingHours">Weekly working hours schedule</param>
    /// <param name="serviceWindow">Requested service window (start and end UTC)</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <param name="jobTimezone">Job location timezone (IANA identifier)</param>
    /// <param name="calendar">Optional contractor calendar with exceptions</param>
    /// <returns>List of available time windows (start/end UTC)</returns>
    IReadOnlyList<TimeWindow> CalculateAvailableWindows(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null);

    /// <summary>
    /// Gets working hours for a specific date, considering calendar exceptions.
    /// </summary>
    /// <param name="workingHours">Weekly working hours schedule</param>
    /// <param name="date">Date to get working hours for</param>
    /// <param name="contractorTimezone">Contractor's timezone (IANA identifier)</param>
    /// <param name="calendar">Optional contractor calendar with exceptions</param>
    /// <returns>Working hours for the date, or null if no working hours or holiday</returns>
    WorkingHours? GetWorkingHoursForDate(
        IReadOnlyList<WorkingHours> workingHours,
        DateOnly date,
        string contractorTimezone,
        ContractorCalendar? calendar = null);
}


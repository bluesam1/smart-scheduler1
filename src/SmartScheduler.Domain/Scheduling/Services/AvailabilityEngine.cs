using SmartScheduler.Domain.Contracts.ValueObjects;
using System.Globalization;
using TimeZoneConverter;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Availability engine that calculates feasible time slots for contractors based on working hours,
/// existing assignments, and calendar exceptions.
/// </summary>
public class AvailabilityEngine : IAvailabilityEngine
{
    /// <inheritdoc />
    public IReadOnlyList<TimeWindow> CalculateAvailableSlots(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null)
    {
        if (workingHours == null || workingHours.Count == 0)
            return Array.Empty<TimeWindow>();

        if (jobDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(jobDurationMinutes), "Job duration must be positive.");

        // Convert service window to contractor timezone for processing
        var contractorTz = GetTimeZone(contractorTimezone);
        var jobTz = GetTimeZone(jobTimezone);

        // Get all available windows from working hours within the service window
        var availableWindows = GetAvailableWindowsFromWorkingHours(
            workingHours,
            serviceWindow,
            contractorTz,
            jobTz,
            calendar);

        // Block out existing assignments
        var blockedWindows = existingAssignments ?? Array.Empty<TimeWindow>();
        var unblockedWindows = RemoveBlockedTime(availableWindows, blockedWindows);

        // Generate feasible slots that fit the job duration
        var feasibleSlots = GenerateFeasibleSlots(unblockedWindows, jobDurationMinutes);

        return feasibleSlots;
    }

    /// <summary>
    /// Gets available time windows from working hours within the service window.
    /// </summary>
    private List<TimeWindow> GetAvailableWindowsFromWorkingHours(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        TimeZoneInfo contractorTz,
        TimeZoneInfo jobTz,
        ContractorCalendar? calendar)
    {
        var availableWindows = new List<TimeWindow>();

        // Convert service window to contractor timezone
        var serviceStartContractor = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.Start, contractorTz);
        var serviceEndContractor = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.End, contractorTz);

        // Get the date range we need to check
        var startDate = serviceStartContractor.Date;
        var endDate = serviceEndContractor.Date;

        // Process each day in the range
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;

            // Check for calendar exceptions first
            if (calendar != null)
            {
                // Check if it's a holiday
                if (calendar.Holidays.Any(h => h == DateOnly.FromDateTime(date)))
                {
                    continue; // Skip holidays
                }

                // Check for override exception
                var exception = calendar.Exceptions.FirstOrDefault(e => e.Date == DateOnly.FromDateTime(date));
                if (exception != null)
                {
                    if (exception.Type == CalendarExceptionType.Holiday)
                    {
                        continue; // Skip holiday exceptions
                    }

                    if (exception.Type == CalendarExceptionType.Override && exception.WorkingHours != null)
                    {
                        // Use override working hours for this day
                        var overrideWindow = GetWorkingHoursWindowForDate(
                            exception.WorkingHours,
                            date,
                            serviceStartContractor,
                            serviceEndContractor,
                            contractorTz);
                        if (overrideWindow != null)
                        {
                            availableWindows.Add(overrideWindow);
                        }
                        continue;
                    }
                }
            }

            // Get working hours for this day of week
            var dayWorkingHours = workingHours.Where(wh => wh.DayOfWeek == dayOfWeek).ToList();
            if (dayWorkingHours.Count == 0)
            {
                continue; // No working hours for this day
            }

            // Process each working hours entry for this day
            foreach (var wh in dayWorkingHours)
            {
                var window = GetWorkingHoursWindowForDate(
                    wh,
                    date,
                    serviceStartContractor,
                    serviceEndContractor,
                    contractorTz);

                if (window != null)
                {
                    availableWindows.Add(window);
                }
            }
        }

        return availableWindows;
    }

    /// <summary>
    /// Gets a time window for a specific date based on working hours, constrained by service window.
    /// </summary>
    private TimeWindow? GetWorkingHoursWindowForDate(
        WorkingHours workingHours,
        DateTime date,
        DateTime serviceStartContractor,
        DateTime serviceEndContractor,
        TimeZoneInfo contractorTz)
    {
        // Combine date with working hours start/end times
        var dayStart = date.Date.Add(workingHours.StartTime.ToTimeSpan());
        var dayEnd = date.Date.Add(workingHours.EndTime.ToTimeSpan());

        // Handle case where end time is next day (e.g., 22:00 - 02:00)
        if (workingHours.EndTime < workingHours.StartTime)
        {
            dayEnd = dayEnd.AddDays(1);
        }

        // Constrain by service window
        var windowStart = dayStart > serviceStartContractor ? dayStart : serviceStartContractor;
        var windowEnd = dayEnd < serviceEndContractor ? dayEnd : serviceEndContractor;

        // Check if there's any overlap
        if (windowStart >= windowEnd)
        {
            return null;
        }

        // Convert back to UTC
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(windowStart, contractorTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(windowEnd, contractorTz);

        return new TimeWindow(startUtc, endUtc);
    }

    /// <summary>
    /// Removes blocked time from available windows.
    /// </summary>
    private List<TimeWindow> RemoveBlockedTime(
        List<TimeWindow> availableWindows,
        IReadOnlyList<TimeWindow> blockedWindows)
    {
        if (blockedWindows.Count == 0)
            return availableWindows;

        var unblockedWindows = new List<TimeWindow>();

        foreach (var availableWindow in availableWindows)
        {
            var remainingWindows = new List<TimeWindow> { availableWindow };

            foreach (var blockedWindow in blockedWindows)
            {
                var newRemaining = new List<TimeWindow>();
                foreach (var remaining in remainingWindows)
                {
                    // Check if there's overlap
                    if (remaining.Start < blockedWindow.End && remaining.End > blockedWindow.Start)
                    {
                        // Split the window around the blocked time
                        if (remaining.Start < blockedWindow.Start)
                        {
                            newRemaining.Add(new TimeWindow(remaining.Start, blockedWindow.Start));
                        }
                        if (remaining.End > blockedWindow.End)
                        {
                            newRemaining.Add(new TimeWindow(blockedWindow.End, remaining.End));
                        }
                    }
                    else
                    {
                        // No overlap, keep the window
                        newRemaining.Add(remaining);
                    }
                }
                remainingWindows = newRemaining;
            }

            unblockedWindows.AddRange(remainingWindows);
        }

        return unblockedWindows;
    }

    /// <summary>
    /// Generates feasible slots from available windows that can fit the job duration.
    /// </summary>
    private List<TimeWindow> GenerateFeasibleSlots(
        List<TimeWindow> availableWindows,
        int jobDurationMinutes)
    {
        var feasibleSlots = new List<TimeWindow>();

        foreach (var window in availableWindows)
        {
            var windowDuration = (int)(window.End - window.Start).TotalMinutes;
            
            if (windowDuration < jobDurationMinutes)
            {
                continue; // Window too short for job
            }

            // Generate slots starting from the beginning of the window
            // For now, generate one slot per window (earliest possible)
            // Later stories will add logic for multiple slot types
            var slotStart = window.Start;
            var slotEnd = slotStart.AddMinutes(jobDurationMinutes);

            if (slotEnd <= window.End)
            {
                feasibleSlots.Add(new TimeWindow(slotStart, slotEnd));
            }
        }

        return feasibleSlots;
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
            // Try to convert IANA to Windows timezone using TimeZoneConverter library
            // If that fails, try direct lookup
            if (TZConvert.TryGetTimeZoneInfo(timezone, out var tz))
            {
                return tz;
            }

            // Fallback to direct lookup (works for Windows timezones)
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid timezone: {timezone}", nameof(timezone));
        }
    }
}


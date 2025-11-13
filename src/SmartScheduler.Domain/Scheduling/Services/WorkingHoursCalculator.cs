using SmartScheduler.Domain.Contracts.ValueObjects;
using System.Globalization;
using TimeZoneConverter;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Calculator for processing working hours and determining available time windows.
/// Handles timezone conversions, calendar exceptions, and break time.
/// </summary>
public class WorkingHoursCalculator : IWorkingHoursCalculator
{
    /// <inheritdoc />
    public IReadOnlyList<TimeWindow> CalculateAvailableWindows(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null)
    {
        if (workingHours == null || workingHours.Count == 0)
            return Array.Empty<TimeWindow>();

        var contractorTz = GetTimeZone(contractorTimezone);
        var jobTz = GetTimeZone(jobTimezone);

        // Convert service window to contractor timezone for processing
        var serviceStartContractor = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.Start, contractorTz);
        var serviceEndContractor = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.End, contractorTz);

        var availableWindows = new List<TimeWindow>();

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

    /// <inheritdoc />
    public WorkingHours? GetWorkingHoursForDate(
        IReadOnlyList<WorkingHours> workingHours,
        DateOnly date,
        string contractorTimezone,
        ContractorCalendar? calendar = null)
    {
        if (workingHours == null || workingHours.Count == 0)
            return null;

        var dayOfWeek = date.DayOfWeek;

        // Check for calendar exceptions first
        if (calendar != null)
        {
            // Check if it's a holiday
            if (calendar.Holidays.Any(h => h == date))
            {
                return null; // No working hours on holidays
            }

            // Check for override exception
            var exception = calendar.Exceptions.FirstOrDefault(e => e.Date == date);
            if (exception != null)
            {
                if (exception.Type == CalendarExceptionType.Holiday)
                {
                    return null; // No working hours on holiday exceptions
                }

                if (exception.Type == CalendarExceptionType.Override && exception.WorkingHours != null)
                {
                    return exception.WorkingHours; // Return override working hours
                }
            }
        }

        // Get working hours for this day of week
        var dayWorkingHours = workingHours.Where(wh => wh.DayOfWeek == dayOfWeek).FirstOrDefault();
        return dayWorkingHours;
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
    /// Gets TimeZoneInfo from IANA timezone identifier.
    /// </summary>
    private TimeZoneInfo GetTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        try
        {
            // Try to convert IANA to Windows timezone using TimeZoneConverter library
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


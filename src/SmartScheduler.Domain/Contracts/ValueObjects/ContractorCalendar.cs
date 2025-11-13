namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents a contractor's calendar with holidays, exceptions, and break settings.
/// Immutable value object.
/// </summary>
public record ContractorCalendar
{
    public IReadOnlyList<DateOnly> Holidays { get; init; }
    public IReadOnlyList<CalendarException> Exceptions { get; init; }
    public int DailyBreakMinutes { get; init; }

    public ContractorCalendar(
        IReadOnlyList<DateOnly>? holidays = null,
        IReadOnlyList<CalendarException>? exceptions = null,
        int dailyBreakMinutes = 30)
    {
        // Validate break minutes
        if (dailyBreakMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyBreakMinutes), "Daily break minutes cannot be negative.");

        Holidays = holidays ?? Array.Empty<DateOnly>();
        Exceptions = exceptions ?? Array.Empty<CalendarException>();
        DailyBreakMinutes = dailyBreakMinutes;
    }
}


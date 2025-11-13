namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents a calendar exception (holiday or override) for a specific date.
/// Immutable value object.
/// </summary>
public record CalendarException
{
    public DateOnly Date { get; init; }
    public CalendarExceptionType Type { get; init; }
    public WorkingHours? WorkingHours { get; init; }

    public CalendarException(
        DateOnly date,
        CalendarExceptionType type,
        WorkingHours? workingHours = null)
    {
        Date = date;
        Type = type;
        WorkingHours = workingHours;

        // If type is override, working hours must be provided
        if (type == CalendarExceptionType.Override && workingHours == null)
            throw new ArgumentException("Working hours must be provided for override exceptions.", nameof(workingHours));
    }
}

/// <summary>
/// Type of calendar exception.
/// </summary>
public enum CalendarExceptionType
{
    Holiday,
    Override
}


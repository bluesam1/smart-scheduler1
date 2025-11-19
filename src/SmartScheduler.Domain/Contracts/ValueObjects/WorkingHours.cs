namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents working hours for a specific day of the week.
/// Immutable value object.
/// </summary>
public record WorkingHours
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string TimeZone { get; init; }

    public WorkingHours(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        string timeZone)
    {
        // Validate day of week (0-6, Sunday-Saturday)
        if (!Enum.IsDefined(typeof(DayOfWeek), dayOfWeek))
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week must be a valid DayOfWeek value.");

        // Validate start time is before end time
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be before end time.", nameof(startTime));

        // Validate timezone is not empty
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new ArgumentException("Time zone cannot be empty.", nameof(timeZone));

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        TimeZone = timeZone;
    }

    /// <summary>
    /// Gets the duration in minutes.
    /// </summary>
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    /// <summary>
    /// Validates that the working hours are valid.
    /// </summary>
    public bool IsValid => StartTime < EndTime && !string.IsNullOrWhiteSpace(TimeZone);
}





namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents a time window with start and end times.
/// Immutable value object.
/// </summary>
public record TimeWindow
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    public TimeWindow(DateTime start, DateTime end)
    {
        // Validate start is before end
        if (start >= end)
            throw new ArgumentException("Start time must be before end time.", nameof(start));

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the duration in minutes.
    /// </summary>
    public int DurationMinutes => (int)(End - Start).TotalMinutes;

    /// <summary>
    /// Validates that the time window is valid.
    /// </summary>
    public bool IsValid => Start < End;
}


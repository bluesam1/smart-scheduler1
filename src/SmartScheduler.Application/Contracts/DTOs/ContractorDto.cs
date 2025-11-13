namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for contractor data.
/// </summary>
public record ContractorDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public GeoLocationDto BaseLocation { get; init; } = null!;
    public int Rating { get; init; }
    public IReadOnlyList<WorkingHoursDto> WorkingHours { get; init; } = Array.Empty<WorkingHoursDto>();
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();
    public ContractorCalendarDto? Calendar { get; init; }
    public string Availability { get; init; } = string.Empty;
    public int JobsToday { get; init; }
    public int MaxJobsPerDay { get; init; }
    public double CurrentUtilization { get; init; }
    public string Timezone { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record GeoLocationDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? FormattedAddress { get; init; }
    public string? PlaceId { get; init; }
}

public record WorkingHoursDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty; // HH:mm format
    public string EndTime { get; init; } = string.Empty; // HH:mm format
    public string TimeZone { get; init; } = string.Empty;
}

public record ContractorCalendarDto
{
    public IReadOnlyList<DateOnly> Holidays { get; init; } = Array.Empty<DateOnly>();
    public IReadOnlyList<CalendarExceptionDto> Exceptions { get; init; } = Array.Empty<CalendarExceptionDto>();
    public int DailyBreakMinutes { get; init; }
}

public record CalendarExceptionDto
{
    public DateOnly Date { get; init; }
    public string Type { get; init; } = string.Empty; // "Holiday" or "Override"
    public WorkingHoursDto? WorkingHours { get; init; }
}


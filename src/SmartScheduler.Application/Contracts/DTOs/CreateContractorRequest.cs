namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Request DTO for creating a new contractor.
/// </summary>
public record CreateContractorRequest
{
    public string Name { get; init; } = string.Empty;
    public GeoLocationDto BaseLocation { get; init; } = null!;
    public IReadOnlyList<WorkingHoursDto> WorkingHours { get; init; } = Array.Empty<WorkingHoursDto>();
    public IReadOnlyList<string>? Skills { get; init; }
    public int? Rating { get; init; }
    public ContractorCalendarDto? Calendar { get; init; }
    public int? MaxJobsPerDay { get; init; }
}


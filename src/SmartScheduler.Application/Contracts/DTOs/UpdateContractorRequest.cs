namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Request DTO for updating an existing contractor.
/// </summary>
public record UpdateContractorRequest
{
    public string? Name { get; init; }
    public GeoLocationDto? BaseLocation { get; init; }
    public IReadOnlyList<WorkingHoursDto>? WorkingHours { get; init; }
    public IReadOnlyList<string>? Skills { get; init; }
    public int? Rating { get; init; }
    public ContractorCalendarDto? Calendar { get; init; }
    public int? MaxJobsPerDay { get; init; }
}


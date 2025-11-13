namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for job types list.
/// </summary>
public record JobTypesResponseDto
{
    public IReadOnlyList<string> JobTypes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Response DTO for skills list.
/// </summary>
public record SkillsResponseDto
{
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Request DTO for adding a job type.
/// </summary>
public record AddJobTypeRequestDto
{
    public string JobType { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for adding a skill.
/// </summary>
public record AddSkillRequestDto
{
    public string Skill { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a job type.
/// </summary>
public record UpdateJobTypeRequestDto
{
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a skill.
/// </summary>
public record UpdateSkillRequestDto
{
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for adding a job type.
/// </summary>
public record AddJobTypeResponseDto
{
    public string JobType { get; init; } = string.Empty;
}


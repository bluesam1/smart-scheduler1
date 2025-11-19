namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for current weights configuration.
/// </summary>
public record WeightsConfigResponseDto
{
    public int Version { get; init; }
    public WeightFactorsDto Weights { get; init; } = null!;
    public List<string> TieBreakers { get; init; } = new();
    public RotationConfigDto Rotation { get; init; } = null!;
    public string ChangeNotes { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for weight factors.
/// </summary>
public record WeightFactorsDto
{
    public double Availability { get; init; }
    public double Rating { get; init; }
    public double Distance { get; init; }
}

/// <summary>
/// DTO for rotation configuration.
/// </summary>
public record RotationConfigDto
{
    public bool Enabled { get; init; }
    public double Boost { get; init; }
    public double UnderUtilizationThreshold { get; init; }
}

/// <summary>
/// Request DTO for updating weights configuration.
/// </summary>
public record UpdateWeightsConfigRequestDto
{
    public WeightFactorsDto Weights { get; init; } = null!;
    public List<string> TieBreakers { get; init; } = new();
    public RotationConfigDto Rotation { get; init; } = null!;
    public string ChangeNotes { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for weights configuration history.
/// </summary>
public record WeightsConfigHistoryItemDto
{
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public string ChangeNotes { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request DTO for rollback.
/// </summary>
public record RollbackWeightsConfigRequestDto
{
    public int Version { get; init; }
    public string ChangeNotes { get; init; } = string.Empty;
}





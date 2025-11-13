namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Response DTO for dashboard statistics.
/// </summary>
public record DashboardStatisticsDto
{
    public StatMetric ActiveContractors { get; init; } = null!;
    public JobStatMetric PendingJobs { get; init; } = null!;
    public TimeMetric AverageAssignmentTime { get; init; } = null!;
    public PercentMetric UtilizationRate { get; init; } = null!;
}

/// <summary>
/// Base metric DTO with value and change indicator.
/// </summary>
public record StatMetric
{
    public int Value { get; init; }
    public string? ChangeIndicator { get; init; } // e.g., "+2 today", "-3 this week"
}

/// <summary>
/// Job statistics metric with breakdown.
/// </summary>
public record JobStatMetric
{
    public int Value { get; init; }
    public int Unassigned { get; init; }
    public string? ChangeIndicator { get; init; }
}

/// <summary>
/// Time-based metric (e.g., average assignment time in minutes).
/// </summary>
public record TimeMetric
{
    public int ValueMinutes { get; init; }
    public string? ChangeIndicator { get; init; }
}

/// <summary>
/// Percentage metric.
/// </summary>
public record PercentMetric
{
    public double Value { get; init; } // 0-100
    public string? ChangeIndicator { get; init; }
}


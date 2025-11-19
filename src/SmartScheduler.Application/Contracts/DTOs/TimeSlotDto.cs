namespace SmartScheduler.Application.Contracts.DTOs;

/// <summary>
/// Represents a feasible time slot for a job assignment.
/// </summary>
public class TimeSlotDto
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Type { get; set; } = string.Empty; // "earliest", "lowest-travel", "highest-confidence"
    public int Confidence { get; set; } // 0-100
}





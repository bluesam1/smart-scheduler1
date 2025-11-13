namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents the status of a job.
/// </summary>
public enum JobStatus
{
    Created = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}


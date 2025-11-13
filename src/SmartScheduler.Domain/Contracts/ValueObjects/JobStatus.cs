namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents the status of a job.
/// </summary>
public enum JobStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Canceled = 3
}


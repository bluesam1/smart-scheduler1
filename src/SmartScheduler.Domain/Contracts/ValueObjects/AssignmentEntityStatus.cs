namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents the status of an Assignment entity.
/// </summary>
public enum AssignmentEntityStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}



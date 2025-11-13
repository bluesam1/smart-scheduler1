namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents the assignment status of a job (computed property).
/// </summary>
public enum AssignmentStatus
{
    Unassigned = 0,
    PartiallyAssigned = 1,
    Assigned = 2
}


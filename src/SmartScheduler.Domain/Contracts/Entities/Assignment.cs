using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// Assignment aggregate root entity.
/// Links a Job to a Contractor with specific start/end times.
/// Created when dispatcher confirms a booking recommendation.
/// </summary>
public class Assignment
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Guid ContractorId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public AssignmentSource Source { get; private set; }
    public Guid? AuditId { get; private set; }
    public AssignmentEntityStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties (for EF Core)
    public Job? Job { get; private set; }
    public Contractor? Contractor { get; private set; }
    public AuditRecommendation? Audit { get; private set; }

    // Domain events
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Assignment() { }

    public Assignment(
        Guid id,
        Guid jobId,
        Guid contractorId,
        DateTime startUtc,
        DateTime endUtc,
        AssignmentSource source,
        Guid? auditId = null)
    {
        // Validate ID
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        // Validate job ID
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));

        // Validate contractor ID
        if (contractorId == Guid.Empty)
            throw new ArgumentException("Contractor ID cannot be empty.", nameof(contractorId));

        // Validate time slot
        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));

        // Validate audit ID if provided
        if (auditId.HasValue && auditId.Value == Guid.Empty)
            throw new ArgumentException("Audit ID cannot be empty if provided.", nameof(auditId));

        Id = id;
        JobId = jobId;
        ContractorId = contractorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Source = source;
        AuditId = auditId;
        Status = AssignmentEntityStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new AssignmentCreated(Id, JobId, ContractorId, StartUtc, EndUtc));
    }

    /// <summary>
    /// Confirms the assignment.
    /// </summary>
    public void Confirm()
    {
        if (Status == AssignmentEntityStatus.Cancelled)
            throw new InvalidOperationException("Cannot confirm a cancelled assignment.");

        if (Status == AssignmentEntityStatus.Completed)
            throw new InvalidOperationException("Cannot confirm a completed assignment.");

        if (Status == AssignmentEntityStatus.Confirmed)
            throw new InvalidOperationException("Assignment is already confirmed.");

        Status = AssignmentEntityStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new AssignmentConfirmed(Id, JobId, ContractorId));
    }

    /// <summary>
    /// Marks the assignment as in progress.
    /// </summary>
    public void MarkInProgress()
    {
        if (Status != AssignmentEntityStatus.Confirmed)
            throw new InvalidOperationException("Assignment must be confirmed before marking as in progress.");

        Status = AssignmentEntityStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the assignment as completed.
    /// </summary>
    public void MarkCompleted()
    {
        if (Status != AssignmentEntityStatus.InProgress)
            throw new InvalidOperationException("Assignment must be in progress before marking as completed.");

        Status = AssignmentEntityStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the assignment.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status == AssignmentEntityStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed assignment.");

        if (Status == AssignmentEntityStatus.Cancelled)
            throw new InvalidOperationException("Assignment is already cancelled.");

        Status = AssignmentEntityStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new AssignmentCancelled(Id, JobId, ContractorId, reason ?? "No reason provided"));
    }

    /// <summary>
    /// Updates the time slot for the assignment.
    /// </summary>
    public void UpdateTimeSlot(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));

        if (Status == AssignmentEntityStatus.Completed)
            throw new InvalidOperationException("Cannot update time slot for a completed assignment.");

        if (Status == AssignmentEntityStatus.Cancelled)
            throw new InvalidOperationException("Cannot update time slot for a cancelled assignment.");

        StartUtc = startUtc;
        EndUtc = endUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Represents the source of an assignment (auto or manual).
/// </summary>
public enum AssignmentSource
{
    Auto = 0,
    Manual = 1
}





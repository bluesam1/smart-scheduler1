using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// Job aggregate root entity.
/// Represents a job with type, location, required skills, service window, and status.
/// </summary>
public class Job
{
    private readonly List<DomainEvent> _domainEvents = new();
    private List<string> _requiredSkills = new();
    private readonly List<ContractorAssignment> _assignments = new();

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Duration { get; private set; } // Duration in minutes
    public GeoLocation Location { get; private set; } = null!;
    public string Timezone { get; private set; } = string.Empty;
    public IReadOnlyList<string> RequiredSkills => _requiredSkills.AsReadOnly();
    public TimeWindow ServiceWindow { get; private set; } = null!;
    public Priority Priority { get; private set; }
    public JobStatus Status { get; private set; }
    public string? AccessNotes { get; private set; }
    public List<string>? Tools { get; private set; }
    public DateTime DesiredDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? LastRecommendationAuditId { get; private set; }

    // Computed properties (these would typically be calculated based on assignments)
    // For now, they are placeholders that would be computed in the application layer
    public AssignmentStatus AssignmentStatus => ComputeAssignmentStatus();
    public IReadOnlyList<ContractorAssignment> AssignedContractors => _assignments.AsReadOnly();

    // Domain events
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Job() { }

    public Job(
        Guid id,
        string type,
        int duration,
        GeoLocation location,
        string timezone,
        TimeWindow serviceWindow,
        Priority priority,
        DateTime desiredDate,
        IReadOnlyList<string>? requiredSkills = null,
        string? description = null,
        string? accessNotes = null,
        List<string>? tools = null)
    {
        // Validate type
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Job type cannot be empty.", nameof(type));

        // Validate duration
        if (duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        // Validate location
        if (location == null)
            throw new ArgumentNullException(nameof(location));
        
        if (!location.IsValid)
            throw new ArgumentException("Location must have valid coordinates and address.", nameof(location));

        // Validate timezone
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        // Validate service window
        if (serviceWindow == null)
            throw new ArgumentNullException(nameof(serviceWindow));
        
        if (!serviceWindow.IsValid)
            throw new ArgumentException("Service window must be valid (start < end).", nameof(serviceWindow));

        // Required skills are optional - no validation needed

        Id = id;
        Type = type;
        Duration = duration;
        Location = location;
        Timezone = timezone;
        ServiceWindow = serviceWindow;
        Priority = priority;
        DesiredDate = desiredDate;
        _requiredSkills = SkillUtilities.Normalize(requiredSkills ?? Array.Empty<string>()).ToList();
        Description = description;
        AccessNotes = accessNotes;
        Tools = tools;
        Status = JobStatus.Scheduled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new JobCreated(Id, Type, Location.FormattedAddress ?? Location.Address ?? "Unknown"));
    }

    /// <summary>
    /// Updates the job type.
    /// </summary>
    public void UpdateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Job type cannot be empty.", nameof(type));

        Type = type;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the job description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the job duration.
    /// </summary>
    public void UpdateDuration(int duration)
    {
        if (duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        Duration = duration;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the job location.
    /// </summary>
    public void UpdateLocation(GeoLocation location, string timezone)
    {
        if (location == null)
            throw new ArgumentNullException(nameof(location));
        
        if (!location.IsValid)
            throw new ArgumentException("Location must have valid coordinates and address.", nameof(location));

        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        Location = location;
        Timezone = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the required skills.
    /// </summary>
    public void UpdateRequiredSkills(IReadOnlyList<string>? requiredSkills)
    {
        // Required skills are optional - allow null or empty
        _requiredSkills = SkillUtilities.Normalize(requiredSkills ?? Array.Empty<string>()).ToList();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the service window.
    /// </summary>
    public void Reschedule(TimeWindow newServiceWindow)
    {
        if (newServiceWindow == null)
            throw new ArgumentNullException(nameof(newServiceWindow));
        
        if (!newServiceWindow.IsValid)
            throw new ArgumentException("Service window must be valid (start < end).", nameof(newServiceWindow));

        var previousStart = ServiceWindow.Start;
        var previousEnd = ServiceWindow.End;

        ServiceWindow = newServiceWindow;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new JobRescheduled(Id, previousStart, previousEnd, newServiceWindow.Start, newServiceWindow.End));
    }

    /// <summary>
    /// Updates the job priority.
    /// </summary>
    public void UpdatePriority(Priority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the job status.
    /// </summary>
    public void UpdateStatus(JobStatus status)
    {
        // Validate status transition
        if (!IsValidStatusTransition(Status, status))
            throw new InvalidOperationException($"Invalid status transition from {Status} to {status}.");

        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a contractor to this job.
    /// </summary>
    public void AssignContractor(Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        if (contractorId == Guid.Empty)
            throw new ArgumentException("Contractor ID cannot be empty.", nameof(contractorId));

        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));

        // Add assignment
        _assignments.Add(new ContractorAssignment(contractorId, startUtc, endUtc));

        // Status remains Scheduled when contractor is assigned
        // (AssignmentStatus will reflect the assignment)
        
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new JobAssigned(Id, contractorId, startUtc, endUtc));
    }

    /// <summary>
    /// Cancels the job.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == JobStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed job.");

        if (Status == JobStatus.Canceled)
            throw new InvalidOperationException("Job is already canceled.");

        Status = JobStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new JobCancelled(Id, reason ?? "No reason provided"));
    }

    /// <summary>
    /// Updates access notes.
    /// </summary>
    public void UpdateAccessNotes(string? accessNotes)
    {
        AccessNotes = accessNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates tools.
    /// </summary>
    public void UpdateTools(List<string>? tools)
    {
        Tools = tools;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates desired date.
    /// </summary>
    public void UpdateDesiredDate(DateTime desiredDate)
    {
        DesiredDate = desiredDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the last recommendation audit ID (reference to cached recommendations).
    /// </summary>
    public void UpdateLastRecommendationAuditId(Guid? auditId)
    {
        LastRecommendationAuditId = auditId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Computes the assignment status based on current assignments.
    /// </summary>
    private AssignmentStatus ComputeAssignmentStatus()
    {
        if (_assignments.Count == 0)
            return AssignmentStatus.Unassigned;

        // This is a simplified computation - in reality, we'd check if assignments cover the full job duration
        // For now, if there are any assignments, we consider it assigned
        return AssignmentStatus.Assigned;
    }

    /// <summary>
    /// Validates status transitions.
    /// </summary>
    private static bool IsValidStatusTransition(JobStatus currentStatus, JobStatus newStatus)
    {
        return currentStatus switch
        {
            JobStatus.Scheduled => newStatus == JobStatus.InProgress || newStatus == JobStatus.Canceled,
            JobStatus.InProgress => newStatus == JobStatus.Completed || newStatus == JobStatus.Canceled,
            JobStatus.Completed => false, // Cannot transition from completed
            JobStatus.Canceled => false, // Cannot transition from canceled
            _ => false
        };
    }
}

/// <summary>
/// Represents a contractor assignment to a job.
/// </summary>
public record ContractorAssignment
{
    public Guid ContractorId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public ContractorAssignment(Guid contractorId, DateTime startUtc, DateTime endUtc)
    {
        ContractorId = contractorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}


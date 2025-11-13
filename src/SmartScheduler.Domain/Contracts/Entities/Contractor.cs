using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;
using static SmartScheduler.Domain.Contracts.ValueObjects.SkillUtilities;

namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// Contractor aggregate root entity.
/// Represents a contractor with skills, location, working hours, and rating.
/// </summary>
public class Contractor
{
    private readonly List<DomainEvent> _domainEvents = new();
    private List<string> _skills = new();
    private readonly List<WorkingHours> _workingHours = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public GeoLocation BaseLocation { get; private set; } = null!;
    public int Rating { get; private set; }
    public IReadOnlyList<WorkingHours> WorkingHours => _workingHours.AsReadOnly();
    public IReadOnlyList<string> Skills => _skills.AsReadOnly();
    public ContractorCalendar? Calendar { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Computed properties (these would typically be calculated based on assignments)
    // For now, they are placeholders that would be computed in the application layer
    public string Availability => "Available"; // TODO: Compute based on current schedule
    public int JobsToday => 0; // TODO: Compute from assignments
    public int MaxJobsPerDay { get; private set; } = 4;
    public double CurrentUtilization => 0.0; // TODO: Compute based on working hours vs assigned time
    public string Timezone => BaseLocation != null && WorkingHours.Any() 
        ? WorkingHours[0].TimeZone 
        : "America/New_York"; // Default timezone

    // Domain events
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Contractor() { }

    public Contractor(
        Guid id,
        string name,
        GeoLocation baseLocation,
        IReadOnlyList<WorkingHours> workingHours,
        IReadOnlyList<string>? skills = null,
        int rating = 50,
        ContractorCalendar? calendar = null,
        int maxJobsPerDay = 4)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Contractor name cannot be empty.", nameof(name));

        // Validate base location
        if (baseLocation == null)
            throw new ArgumentNullException(nameof(baseLocation));
        
        if (!baseLocation.IsValid)
            throw new ArgumentException("Base location must have valid coordinates.", nameof(baseLocation));

        // Validate working hours
        if (workingHours == null || workingHours.Count == 0)
            throw new ArgumentException("Contractor must have at least one working hours entry.", nameof(workingHours));

        // Validate rating
        if (rating < 0 || rating > 100)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 0 and 100.");

        // Validate max jobs per day
        if (maxJobsPerDay < 1)
            throw new ArgumentOutOfRangeException(nameof(maxJobsPerDay), "Max jobs per day must be at least 1.");

        Id = id;
        Name = name;
        BaseLocation = baseLocation;
        Rating = rating;
        _workingHours.Clear();
        _workingHours.AddRange(workingHours);
        _skills = NormalizeSkills(skills ?? Array.Empty<string>());
        Calendar = calendar;
        MaxJobsPerDay = maxJobsPerDay;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _domainEvents.Add(new ContractorCreated(Id, Name));
    }

    /// <summary>
    /// Updates the contractor's name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Contractor name cannot be empty.", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ContractorUpdated(Id, Name));
    }

    /// <summary>
    /// Updates the contractor's rating.
    /// </summary>
    public void UpdateRating(int newRating)
    {
        if (newRating < 0 || newRating > 100)
            throw new ArgumentOutOfRangeException(nameof(newRating), "Rating must be between 0 and 100.");

        var previousRating = Rating;
        Rating = newRating;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ContractorRated(Id, previousRating, newRating));
    }

    /// <summary>
    /// Updates the contractor's skills.
    /// </summary>
    public void UpdateSkills(IReadOnlyList<string> skills)
    {
        _skills = NormalizeSkills(skills ?? Array.Empty<string>());
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the contractor's base location.
    /// </summary>
    public void UpdateBaseLocation(GeoLocation baseLocation)
    {
        if (baseLocation == null)
            throw new ArgumentNullException(nameof(baseLocation));
        
        if (!baseLocation.IsValid)
            throw new ArgumentException("Base location must have valid coordinates.", nameof(baseLocation));

        BaseLocation = baseLocation;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the contractor's working hours.
    /// </summary>
    public void UpdateWorkingHours(IReadOnlyList<WorkingHours> workingHours)
    {
        if (workingHours == null || workingHours.Count == 0)
            throw new ArgumentException("Contractor must have at least one working hours entry.", nameof(workingHours));

        _workingHours.Clear();
        _workingHours.AddRange(workingHours);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the contractor's calendar.
    /// </summary>
    public void UpdateCalendar(ContractorCalendar? calendar)
    {
        Calendar = calendar;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the maximum jobs per day.
    /// </summary>
    public void UpdateMaxJobsPerDay(int maxJobsPerDay)
    {
        if (maxJobsPerDay < 1)
            throw new ArgumentOutOfRangeException(nameof(maxJobsPerDay), "Max jobs per day must be at least 1.");

        MaxJobsPerDay = maxJobsPerDay;
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
    /// Normalizes skills by trimming and converting to lowercase.
    /// </summary>
    private static List<string> NormalizeSkills(IReadOnlyList<string> skills)
    {
        return SkillUtilities.Normalize(skills).ToList();
    }
}


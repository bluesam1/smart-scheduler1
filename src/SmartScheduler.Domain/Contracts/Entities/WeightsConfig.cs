namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// WeightsConfig entity for storing scoring weights configuration with versioning.
/// </summary>
public class WeightsConfig
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public string ConfigJson { get; private set; } = string.Empty;
    public string ChangeNotes { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private WeightsConfig() { }

    public WeightsConfig(
        Guid id,
        int version,
        string configJson,
        string changeNotes,
        string createdBy,
        bool isActive = false)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        if (version < 1)
            throw new ArgumentException("Version must be >= 1.", nameof(version));

        if (string.IsNullOrWhiteSpace(configJson))
            throw new ArgumentException("ConfigJson cannot be empty.", nameof(configJson));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be empty.", nameof(createdBy));

        Id = id;
        Version = version;
        ConfigJson = configJson;
        ChangeNotes = changeNotes;
        CreatedBy = createdBy;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this configuration as active.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Marks this configuration as inactive.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}



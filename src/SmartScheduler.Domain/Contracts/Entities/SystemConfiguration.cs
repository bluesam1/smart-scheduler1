namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// SystemConfiguration entity.
/// Stores system-wide configuration values such as available job types and skills.
/// </summary>
public class SystemConfiguration
{
    public Guid Id { get; private set; }
    public ConfigurationType Type { get; private set; }
    
    // Property for EF Core (will be converted to/from JSON)
    public List<string> Values { get; private set; } = new();
    
    // Read-only property for domain access
    public IReadOnlyList<string> ValuesReadOnly => Values.AsReadOnly();
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    // Private constructor for EF Core
    private SystemConfiguration() { }

    public SystemConfiguration(
        Guid id,
        ConfigurationType type,
        IReadOnlyList<string> values,
        string updatedBy)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        if (values == null || values.Count == 0)
            throw new ArgumentException("Values cannot be null or empty.", nameof(values));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(updatedBy));

        // Validate values are not empty
        var nonEmptyValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmptyValues.Count == 0)
            throw new ArgumentException("At least one non-empty value is required.", nameof(values));

        Id = id;
        Type = type;
        Values = nonEmptyValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the configuration values.
    /// </summary>
    public void UpdateValues(IReadOnlyList<string> values, string updatedBy)
    {
        if (values == null || values.Count == 0)
            throw new ArgumentException("Values cannot be null or empty.", nameof(values));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(updatedBy));

        var nonEmptyValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (nonEmptyValues.Count == 0)
            throw new ArgumentException("At least one non-empty value is required.", nameof(values));

        Values = nonEmptyValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a value to the configuration.
    /// </summary>
    public void AddValue(string value, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty.", nameof(value));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(updatedBy));

        var normalizedValue = value.Trim();
        if (Values.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Value '{normalizedValue}' already exists.");

        Values.Add(normalizedValue);
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a value from the configuration.
    /// </summary>
    public void RemoveValue(string value, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty.", nameof(value));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(updatedBy));

        var normalizedValue = value.Trim();
        var existingValue = Values.FirstOrDefault(v => v.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase));
        if (existingValue == null)
            throw new InvalidOperationException($"Value '{normalizedValue}' not found.");

        Values.Remove(existingValue);
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates a value (renames it).
    /// </summary>
    public void UpdateValue(string oldValue, string newValue, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(oldValue))
            throw new ArgumentException("Old value cannot be empty.", nameof(oldValue));

        if (string.IsNullOrWhiteSpace(newValue))
            throw new ArgumentException("New value cannot be empty.", nameof(newValue));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(updatedBy));

        var normalizedOldValue = oldValue.Trim();
        var normalizedNewValue = newValue.Trim();

        if (normalizedOldValue.Equals(normalizedNewValue, StringComparison.OrdinalIgnoreCase))
            return; // No change needed

        var existingValue = Values.FirstOrDefault(v => v.Equals(normalizedOldValue, StringComparison.OrdinalIgnoreCase));
        if (existingValue == null)
            throw new InvalidOperationException($"Value '{normalizedOldValue}' not found.");

        if (Values.Contains(normalizedNewValue, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Value '{normalizedNewValue}' already exists.");

        var index = Values.IndexOf(existingValue);
        Values[index] = normalizedNewValue;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Configuration type enumeration.
/// </summary>
public enum ConfigurationType
{
    JobTypes = 1,
    Skills = 2
}


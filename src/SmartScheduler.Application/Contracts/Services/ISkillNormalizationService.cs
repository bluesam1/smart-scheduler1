namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for normalizing and validating skills.
/// </summary>
public interface ISkillNormalizationService
{
    /// <summary>
    /// Normalizes a single skill string (trim, lowercase).
    /// </summary>
    string Normalize(string skill);

    /// <summary>
    /// Normalizes a collection of skills.
    /// </summary>
    IReadOnlyList<string> Normalize(IReadOnlyList<string> skills);

    /// <summary>
    /// Validates that all skills exist in the system configuration.
    /// </summary>
    /// <param name="skills">Skills to validate</param>
    /// <returns>List of invalid skills (empty if all valid)</returns>
    Task<IReadOnlyList<string>> ValidateAgainstConfigurationAsync(IReadOnlyList<string> skills, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a set of required skills is a subset of available skills (case-insensitive).
    /// </summary>
    bool IsSubset(IReadOnlyList<string> requiredSkills, IReadOnlyList<string> availableSkills);
}


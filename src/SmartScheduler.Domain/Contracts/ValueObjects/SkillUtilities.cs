namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Utility methods for skill operations.
/// </summary>
public static class SkillUtilities
{
    /// <summary>
    /// Normalizes a skill string (trim, lowercase).
    /// </summary>
    public static string Normalize(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return string.Empty;

        return skill.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a collection of skills.
    /// </summary>
    public static IReadOnlyList<string> Normalize(IReadOnlyList<string> skills)
    {
        if (skills == null || skills.Count == 0)
            return Array.Empty<string>();

        return skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Normalize(s))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Checks if a set of required skills is a subset of available skills (case-insensitive).
    /// </summary>
    public static bool IsSubset(IReadOnlyList<string> requiredSkills, IReadOnlyList<string> availableSkills)
    {
        if (requiredSkills == null || requiredSkills.Count == 0)
            return true; // Empty set is always a subset

        if (availableSkills == null || availableSkills.Count == 0)
            return false; // Required skills but no available skills

        // Normalize both sets for case-insensitive comparison
        var normalizedRequired = Normalize(requiredSkills).ToHashSet();
        var normalizedAvailable = Normalize(availableSkills).ToHashSet();

        // Check if all required skills are in available skills
        return normalizedRequired.IsSubsetOf(normalizedAvailable);
    }
}


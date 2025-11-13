namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Implementation of skill normalization service.
/// </summary>
public class SkillNormalizationService : ISkillNormalizationService
{
    public string Normalize(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return string.Empty;

        return skill.Trim().ToLowerInvariant();
    }

    public IReadOnlyList<string> Normalize(IReadOnlyList<string> skills)
    {
        if (skills == null || skills.Count == 0)
            return Array.Empty<string>();

        return skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Normalize(s))
            .Distinct()
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ValidateAgainstConfigurationAsync(
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement validation against SystemConfiguration when story 9.1 is complete
        // For now, return empty list (all skills considered valid)
        await Task.CompletedTask;
        return Array.Empty<string>();
    }

    public bool IsSubset(IReadOnlyList<string> requiredSkills, IReadOnlyList<string> availableSkills)
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


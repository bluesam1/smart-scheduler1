using MediatR;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to delete a skill.
/// </summary>
public record DeleteSkillCommand : IRequest<Unit>
{
    public string Skill { get; init; } = string.Empty;
    public string UpdatedBy { get; init; } = string.Empty;
}


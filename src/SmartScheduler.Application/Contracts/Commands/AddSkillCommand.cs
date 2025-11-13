using MediatR;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to add a new skill.
/// </summary>
public record AddSkillCommand : IRequest<Unit>
{
    public string Skill { get; init; } = string.Empty;
    public string UpdatedBy { get; init; } = string.Empty;
}


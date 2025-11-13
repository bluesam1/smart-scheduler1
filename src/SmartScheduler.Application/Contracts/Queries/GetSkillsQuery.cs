using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get all skills.
/// </summary>
public record GetSkillsQuery : IRequest<SkillsResponseDto>;


using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get recent activities.
/// </summary>
public record GetActivitiesQuery : IRequest<IReadOnlyList<ActivityDto>>
{
    public IReadOnlyList<string>? Types { get; init; } // Filter by activity types
    public int Limit { get; init; } = 20; // Default 20, max 100
}


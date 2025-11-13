using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get dashboard statistics.
/// </summary>
public record GetDashboardStatisticsQuery : IRequest<DashboardStatisticsDto>
{
}



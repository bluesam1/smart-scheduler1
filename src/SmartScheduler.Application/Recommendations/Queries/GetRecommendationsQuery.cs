using MediatR;
using SmartScheduler.Application.Recommendations.DTOs;

namespace SmartScheduler.Application.Recommendations.Queries;

/// <summary>
/// Query to get contractor recommendations for a job.
/// </summary>
public record GetRecommendationsQuery : IRequest<RecommendationResponse>
{
    public Guid JobId { get; init; }
    public DateOnly DesiredDate { get; init; }
    public TimeWindowDto? ServiceWindow { get; init; }
    public int MaxResults { get; init; } = 10;
}


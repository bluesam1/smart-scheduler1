using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get all jobs with optional filtering.
/// </summary>
public record GetJobsQuery : IRequest<IReadOnlyList<JobDto>>
{
    public string? Status { get; init; } // "Created", "Assigned", "InProgress", "Completed", "Cancelled"
    public string? Priority { get; init; } // "Normal", "High", "Rush"
    public int? Limit { get; init; }
}


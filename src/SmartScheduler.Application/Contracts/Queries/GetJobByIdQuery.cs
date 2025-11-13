using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get a job by ID.
/// </summary>
public record GetJobByIdQuery : IRequest<JobDto?>
{
    public Guid Id { get; init; }
}



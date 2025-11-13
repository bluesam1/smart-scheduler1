using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update a job's status.
/// </summary>
public record UpdateJobStatusCommand : IRequest<JobDto>
{
    public Guid JobId { get; init; }
    public JobStatus NewStatus { get; init; }
}


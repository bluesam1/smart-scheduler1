using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update an existing job.
/// </summary>
public record UpdateJobCommand : IRequest<JobDto>
{
    public Guid Id { get; init; }
    public UpdateJobRequest Request { get; init; } = null!;
}



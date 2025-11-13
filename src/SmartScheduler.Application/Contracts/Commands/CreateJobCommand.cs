using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to create a new job.
/// </summary>
public record CreateJobCommand : IRequest<JobDto>
{
    public CreateJobRequest Request { get; init; } = null!;
}


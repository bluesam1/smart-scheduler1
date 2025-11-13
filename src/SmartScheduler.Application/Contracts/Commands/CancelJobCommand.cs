using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to cancel a job.
/// </summary>
public record CancelJobCommand : IRequest<JobDto>
{
    public Guid JobId { get; init; }
    public CancelJobRequest? Request { get; init; }
}

/// <summary>
/// Request DTO for cancelling a job.
/// </summary>
public record CancelJobRequest
{
    public string? Reason { get; init; }
}


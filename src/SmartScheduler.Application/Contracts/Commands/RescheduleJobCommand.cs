using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to reschedule a job to a new time slot.
/// </summary>
public record RescheduleJobCommand : IRequest<JobDto>
{
    public Guid JobId { get; init; }
    public RescheduleJobRequest Request { get; init; } = null!;
}

/// <summary>
/// Request DTO for rescheduling a job.
/// </summary>
public record RescheduleJobRequest
{
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
}


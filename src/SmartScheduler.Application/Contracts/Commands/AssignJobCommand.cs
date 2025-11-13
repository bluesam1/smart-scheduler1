using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to assign a job to a contractor.
/// </summary>
public record AssignJobCommand : IRequest<AssignmentDto>
{
    public Guid JobId { get; init; }
    public AssignJobRequest Request { get; init; } = null!;
}


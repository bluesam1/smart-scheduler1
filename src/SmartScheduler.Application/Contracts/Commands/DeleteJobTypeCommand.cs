using MediatR;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to delete a job type.
/// </summary>
public record DeleteJobTypeCommand : IRequest<Unit>
{
    public string JobType { get; init; } = string.Empty;
    public string UpdatedBy { get; init; } = string.Empty;
}


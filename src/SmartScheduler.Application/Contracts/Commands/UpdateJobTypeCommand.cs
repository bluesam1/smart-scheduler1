using MediatR;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update a job type (rename).
/// </summary>
public record UpdateJobTypeCommand : IRequest<Unit>
{
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public string UpdatedBy { get; init; } = string.Empty;
}


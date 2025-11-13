using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to rollback to a previous weights configuration version.
/// </summary>
public record RollbackWeightsConfigCommand : IRequest<WeightsConfigResponseDto>
{
    public RollbackWeightsConfigRequestDto Request { get; init; } = null!;
    public string CreatedBy { get; init; } = string.Empty;
}



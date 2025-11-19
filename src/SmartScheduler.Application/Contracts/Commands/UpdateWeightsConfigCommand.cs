using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update weights configuration (creates new version).
/// </summary>
public record UpdateWeightsConfigCommand : IRequest<WeightsConfigResponseDto>
{
    public UpdateWeightsConfigRequestDto Request { get; init; } = null!;
    public string CreatedBy { get; init; } = string.Empty;
}





using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to add a new job type.
/// </summary>
public record AddJobTypeCommand : IRequest<AddJobTypeResponseDto>
{
    public AddJobTypeRequestDto Request { get; init; } = null!;
    public string UpdatedBy { get; init; } = string.Empty;
}


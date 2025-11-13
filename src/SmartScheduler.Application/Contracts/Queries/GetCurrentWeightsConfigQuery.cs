using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get the current active weights configuration.
/// </summary>
public record GetCurrentWeightsConfigQuery : IRequest<WeightsConfigResponseDto?>;



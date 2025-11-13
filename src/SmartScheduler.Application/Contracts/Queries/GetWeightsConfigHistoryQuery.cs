using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get weights configuration history.
/// </summary>
public record GetWeightsConfigHistoryQuery : IRequest<IReadOnlyList<WeightsConfigHistoryItemDto>>;


using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get all contractors with optional skill filtering.
/// </summary>
public record GetContractorsQuery : IRequest<IReadOnlyList<ContractorDto>>
{
    public IReadOnlyList<string>? Skills { get; init; }
    public int? Limit { get; init; }
}



using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Queries;

/// <summary>
/// Query to get a contractor by ID.
/// </summary>
public record GetContractorByIdQuery : IRequest<ContractorDto?>
{
    public Guid Id { get; init; }
}



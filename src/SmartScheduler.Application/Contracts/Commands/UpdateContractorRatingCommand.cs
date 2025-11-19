using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update a contractor's rating.
/// </summary>
public record UpdateContractorRatingCommand : IRequest<ContractorDto>
{
    public Guid ContractorId { get; init; }
    public int Rating { get; init; }
}





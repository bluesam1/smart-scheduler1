using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to create a new contractor.
/// </summary>
public record CreateContractorCommand : IRequest<ContractorDto>
{
    public CreateContractorRequest Request { get; init; } = null!;
}


using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update an existing contractor.
/// </summary>
public record UpdateContractorCommand : IRequest<ContractorDto>
{
    public Guid Id { get; init; }
    public UpdateContractorRequest Request { get; init; } = null!;
}


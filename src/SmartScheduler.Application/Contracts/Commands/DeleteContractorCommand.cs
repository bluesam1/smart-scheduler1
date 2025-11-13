using MediatR;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to delete a contractor.
/// </summary>
public record DeleteContractorCommand : IRequest
{
    public Guid Id { get; init; }
}



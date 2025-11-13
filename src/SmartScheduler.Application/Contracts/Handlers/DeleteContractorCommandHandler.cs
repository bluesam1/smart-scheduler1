using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for DeleteContractorCommand.
/// </summary>
public class DeleteContractorCommandHandler : IRequestHandler<DeleteContractorCommand>
{
    private readonly IContractorRepository _repository;

    public DeleteContractorCommandHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteContractorCommand request, CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.Id} not found.");
        }

        await _repository.DeleteAsync(request.Id, cancellationToken);
    }
}


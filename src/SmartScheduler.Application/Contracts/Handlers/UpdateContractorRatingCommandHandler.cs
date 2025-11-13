using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateContractorRatingCommand.
/// </summary>
public class UpdateContractorRatingCommandHandler : IRequestHandler<UpdateContractorRatingCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;

    public UpdateContractorRatingCommandHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto> Handle(
        UpdateContractorRatingCommand request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.ContractorId, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.ContractorId} not found.");
        }

        // Update rating (validates 0-100 and publishes ContractorRated event)
        contractor.UpdateRating(request.Rating);

        await _repository.UpdateAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}



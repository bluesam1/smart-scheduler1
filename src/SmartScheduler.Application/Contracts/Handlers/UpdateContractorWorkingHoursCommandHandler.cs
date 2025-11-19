using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateContractorWorkingHoursCommand.
/// </summary>
public class UpdateContractorWorkingHoursCommandHandler : IRequestHandler<UpdateContractorWorkingHoursCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;

    public UpdateContractorWorkingHoursCommandHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto> Handle(
        UpdateContractorWorkingHoursCommand request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.ContractorId, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.ContractorId} not found.");
        }

        if (request.WorkingHours == null || request.WorkingHours.Count == 0)
        {
            throw new ArgumentException("At least one working hours entry is required.", nameof(request));
        }

        var workingHours = request.WorkingHours.Select(wh => wh.ToDomain()).ToList();
        contractor.UpdateWorkingHours(workingHours);

        await _repository.UpdateAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}





using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for RemoveCalendarExceptionCommand.
/// </summary>
public class RemoveCalendarExceptionCommandHandler : IRequestHandler<RemoveCalendarExceptionCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;

    public RemoveCalendarExceptionCommandHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto> Handle(
        RemoveCalendarExceptionCommand request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.ContractorId, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.ContractorId} not found.");
        }

        var currentCalendar = contractor.Calendar ?? new ContractorCalendar();
        
        // Remove exception with matching date
        var exceptions = currentCalendar.Exceptions
            .Where(e => e.Date != request.Date)
            .ToList();

        var updatedCalendar = new ContractorCalendar(
            holidays: currentCalendar.Holidays.ToList(),
            exceptions: exceptions,
            dailyBreakMinutes: currentCalendar.DailyBreakMinutes);

        contractor.UpdateCalendar(updatedCalendar);

        await _repository.UpdateAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}


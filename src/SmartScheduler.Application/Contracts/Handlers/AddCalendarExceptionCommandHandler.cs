using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for AddCalendarExceptionCommand.
/// </summary>
public class AddCalendarExceptionCommandHandler : IRequestHandler<AddCalendarExceptionCommand, ContractorDto>
{
    private readonly IContractorRepository _repository;

    public AddCalendarExceptionCommandHandler(IContractorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto> Handle(
        AddCalendarExceptionCommand request,
        CancellationToken cancellationToken)
    {
        var contractor = await _repository.GetByIdAsync(request.ContractorId, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {request.ContractorId} not found.");
        }

        var exception = request.Exception.ToDomain();
        var currentCalendar = contractor.Calendar ?? new ContractorCalendar();
        
        // Add exception to existing list
        var exceptions = currentCalendar.Exceptions.ToList();
        exceptions.Add(exception);

        var updatedCalendar = new ContractorCalendar(
            holidays: currentCalendar.Holidays.ToList(),
            exceptions: exceptions,
            dailyBreakMinutes: currentCalendar.DailyBreakMinutes);

        contractor.UpdateCalendar(updatedCalendar);

        await _repository.UpdateAsync(contractor, cancellationToken);

        return contractor.ToDto();
    }
}


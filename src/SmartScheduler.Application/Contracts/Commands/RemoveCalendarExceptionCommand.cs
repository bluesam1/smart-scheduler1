using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to remove a calendar exception from a contractor's calendar.
/// </summary>
public record RemoveCalendarExceptionCommand : IRequest<ContractorDto>
{
    public Guid ContractorId { get; init; }
    public DateOnly Date { get; init; }
}


using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to add a calendar exception to a contractor's calendar.
/// </summary>
public record AddCalendarExceptionCommand : IRequest<ContractorDto>
{
    public Guid ContractorId { get; init; }
    public CalendarExceptionDto Exception { get; init; } = null!;
}



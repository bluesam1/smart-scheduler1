using MediatR;
using SmartScheduler.Application.Contracts.DTOs;

namespace SmartScheduler.Application.Contracts.Commands;

/// <summary>
/// Command to update a contractor's working hours.
/// </summary>
public record UpdateContractorWorkingHoursCommand : IRequest<ContractorDto>
{
    public Guid ContractorId { get; init; }
    public IReadOnlyList<WorkingHoursDto> WorkingHours { get; init; } = Array.Empty<WorkingHoursDto>();
}





using MediatR;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetJobTypesQuery.
/// </summary>
public class GetJobTypesQueryHandler : IRequestHandler<GetJobTypesQuery, JobTypesResponseDto>
{
    private readonly ISystemConfigurationRepository _repository;

    public GetJobTypesQueryHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<JobTypesResponseDto> Handle(
        GetJobTypesQuery request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.JobTypes, cancellationToken);
        
        if (config == null)
        {
            // Return empty list if no configuration exists yet
            return new JobTypesResponseDto { JobTypes = Array.Empty<string>() };
        }

        return new JobTypesResponseDto { JobTypes = config.ValuesReadOnly };
    }
}


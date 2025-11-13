using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for AddJobTypeCommand.
/// </summary>
public class AddJobTypeCommandHandler : IRequestHandler<AddJobTypeCommand, AddJobTypeResponseDto>
{
    private readonly ISystemConfigurationRepository _repository;

    public AddJobTypeCommandHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<AddJobTypeResponseDto> Handle(
        AddJobTypeCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.JobTypes, cancellationToken);

        if (config == null)
        {
            // Create new configuration if it doesn't exist
            config = new SystemConfiguration(
                id: Guid.NewGuid(),
                type: ConfigurationType.JobTypes,
                values: new[] { request.Request.JobType },
                updatedBy: request.UpdatedBy);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            // Add value to existing configuration
            config.AddValue(request.Request.JobType, request.UpdatedBy);
            await _repository.UpdateAsync(config, cancellationToken);
        }

        return new AddJobTypeResponseDto { JobType = request.Request.JobType };
    }
}


using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for AddSkillCommand.
/// </summary>
public class AddSkillCommandHandler : IRequestHandler<AddSkillCommand, Unit>
{
    private readonly ISystemConfigurationRepository _repository;

    public AddSkillCommandHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        AddSkillCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.Skills, cancellationToken);

        if (config == null)
        {
            // Create new configuration if it doesn't exist
            config = new SystemConfiguration(
                id: Guid.NewGuid(),
                type: ConfigurationType.Skills,
                values: new[] { request.Skill },
                updatedBy: request.UpdatedBy);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            // Add value to existing configuration
            config.AddValue(request.Skill, request.UpdatedBy);
            await _repository.UpdateAsync(config, cancellationToken);
        }

        return Unit.Value;
    }
}


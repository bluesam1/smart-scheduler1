using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for DeleteSkillCommand.
/// </summary>
public class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand, Unit>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IJobRepository _jobRepository;
    private readonly IContractorRepository _contractorRepository;

    public DeleteSkillCommandHandler(
        ISystemConfigurationRepository repository,
        IJobRepository jobRepository,
        IContractorRepository contractorRepository)
    {
        _repository = repository;
        _jobRepository = jobRepository;
        _contractorRepository = contractorRepository;
    }

    public async Task<Unit> Handle(
        DeleteSkillCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.Skills, cancellationToken);
        if (config == null)
        {
            throw new KeyNotFoundException("Skills configuration not found.");
        }

        // Check if skill is in use by jobs
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var jobsUsingSkill = jobs.Any(j => j.RequiredSkills.Any(s => s.Equals(request.Skill, StringComparison.OrdinalIgnoreCase)));
        
        // Check if skill is in use by contractors
        var contractors = await _contractorRepository.GetAllAsync(cancellationToken);
        var contractorsUsingSkill = contractors.Any(c => c.Skills.Any(s => s.Equals(request.Skill, StringComparison.OrdinalIgnoreCase)));
        
        if (jobsUsingSkill || contractorsUsingSkill)
        {
            throw new InvalidOperationException($"Skill '{request.Skill}' is in use and cannot be deleted.");
        }

        // Remove the value from configuration
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(request.UpdatedBy));
        }
        
        config.RemoveValue(request.Skill, request.UpdatedBy);
        await _repository.UpdateAsync(config, cancellationToken);

        return Unit.Value;
    }
}


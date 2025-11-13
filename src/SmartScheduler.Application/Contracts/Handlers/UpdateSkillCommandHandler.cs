using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateSkillCommand.
/// </summary>
public class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, Unit>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IJobRepository _jobRepository;
    private readonly IContractorRepository _contractorRepository;

    public UpdateSkillCommandHandler(
        ISystemConfigurationRepository repository,
        IJobRepository jobRepository,
        IContractorRepository contractorRepository)
    {
        _repository = repository;
        _jobRepository = jobRepository;
        _contractorRepository = contractorRepository;
    }

    public async Task<Unit> Handle(
        UpdateSkillCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.Skills, cancellationToken);
        if (config == null)
        {
            throw new KeyNotFoundException("Skills configuration not found.");
        }

        // Update the value in configuration
        config.UpdateValue(request.OldValue, request.NewValue, request.UpdatedBy);
        await _repository.UpdateAsync(config, cancellationToken);

        // Update all jobs that use the old skill
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var jobsToUpdate = jobs.Where(j => j.RequiredSkills.Any(s => s.Equals(request.OldValue, StringComparison.OrdinalIgnoreCase))).ToList();
        
        foreach (var job in jobsToUpdate)
        {
            var updatedSkills = job.RequiredSkills
                .Select(s => s.Equals(request.OldValue, StringComparison.OrdinalIgnoreCase) ? request.NewValue : s)
                .ToList();
            job.UpdateRequiredSkills(updatedSkills);
            await _jobRepository.UpdateAsync(job, cancellationToken);
        }

        // Update all contractors that have the old skill
        var contractors = await _contractorRepository.GetAllAsync(cancellationToken);
        var contractorsToUpdate = contractors.Where(c => c.Skills.Any(s => s.Equals(request.OldValue, StringComparison.OrdinalIgnoreCase))).ToList();
        
        foreach (var contractor in contractorsToUpdate)
        {
            var updatedSkills = contractor.Skills
                .Select(s => s.Equals(request.OldValue, StringComparison.OrdinalIgnoreCase) ? request.NewValue : s)
                .ToList();
            contractor.UpdateSkills(updatedSkills);
            await _contractorRepository.UpdateAsync(contractor, cancellationToken);
        }

        return Unit.Value;
    }
}


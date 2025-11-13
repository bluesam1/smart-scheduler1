using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateJobTypeCommand.
/// </summary>
public class UpdateJobTypeCommandHandler : IRequestHandler<UpdateJobTypeCommand, Unit>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IJobRepository _jobRepository;

    public UpdateJobTypeCommandHandler(
        ISystemConfigurationRepository repository,
        IJobRepository jobRepository)
    {
        _repository = repository;
        _jobRepository = jobRepository;
    }

    public async Task<Unit> Handle(
        UpdateJobTypeCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.JobTypes, cancellationToken);
        if (config == null)
        {
            throw new KeyNotFoundException("Job types configuration not found.");
        }

        // Update the value in configuration
        config.UpdateValue(request.OldValue, request.NewValue, request.UpdatedBy);
        await _repository.UpdateAsync(config, cancellationToken);

        // Update all jobs that use the old job type
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var jobsToUpdate = jobs.Where(j => j.Type.Equals(request.OldValue, StringComparison.OrdinalIgnoreCase)).ToList();
        
        foreach (var job in jobsToUpdate)
        {
            job.UpdateType(request.NewValue);
            await _jobRepository.UpdateAsync(job, cancellationToken);
        }

        return Unit.Value;
    }
}


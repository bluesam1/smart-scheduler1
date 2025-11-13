using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for DeleteJobTypeCommand.
/// </summary>
public class DeleteJobTypeCommandHandler : IRequestHandler<DeleteJobTypeCommand, Unit>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IJobRepository _jobRepository;

    public DeleteJobTypeCommandHandler(
        ISystemConfigurationRepository repository,
        IJobRepository jobRepository)
    {
        _repository = repository;
        _jobRepository = jobRepository;
    }

    public async Task<Unit> Handle(
        DeleteJobTypeCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTypeAsync(ConfigurationType.JobTypes, cancellationToken);
        if (config == null)
        {
            throw new KeyNotFoundException("Job types configuration not found.");
        }

        // Check if job type is in use
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var jobsUsingType = jobs.Any(j => j.Type.Equals(request.JobType, StringComparison.OrdinalIgnoreCase));
        
        if (jobsUsingType)
        {
            throw new InvalidOperationException($"Job type '{request.JobType}' is in use and cannot be deleted.");
        }

        // Remove the value from configuration
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            throw new ArgumentException("UpdatedBy cannot be empty.", nameof(request.UpdatedBy));
        }
        
        config.RemoveValue(request.JobType, request.UpdatedBy);
        await _repository.UpdateAsync(config, cancellationToken);

        return Unit.Value;
    }
}


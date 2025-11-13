using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Domain.Contracts.Repositories;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for UpdateJobStatusCommand.
/// </summary>
public class UpdateJobStatusCommandHandler : IRequestHandler<UpdateJobStatusCommand, JobDto>
{
    private readonly IJobRepository _jobRepository;

    public UpdateJobStatusCommandHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobDto> Handle(UpdateJobStatusCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        
        if (job == null)
        {
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");
        }

        // Update status (validates transition internally)
        job.UpdateStatus(request.NewStatus);

        // Save changes
        await _jobRepository.UpdateAsync(job, cancellationToken);

        // Return updated job
        return job.ToDto();
    }
}


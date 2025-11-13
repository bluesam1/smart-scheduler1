using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for CancelJobCommand.
/// </summary>
public class CancelJobCommandHandler : IRequestHandler<CancelJobCommand, JobDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ICalendarConsistencyChecker _consistencyChecker;

    public CancelJobCommandHandler(
        IJobRepository jobRepository,
        IAssignmentRepository assignmentRepository,
        IRealtimePublisher realtimePublisher,
        ICalendarConsistencyChecker consistencyChecker)
    {
        _jobRepository = jobRepository;
        _assignmentRepository = assignmentRepository;
        _realtimePublisher = realtimePublisher;
        _consistencyChecker = consistencyChecker;
    }

    public async Task<JobDto> Handle(
        CancelJobCommand request,
        CancellationToken cancellationToken)
    {
        // Validate job exists
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");
        }

        // Cancel the job (this raises JobCancelled domain event)
        var reason = request.Request?.Reason ?? "No reason provided";
        job.Cancel(reason);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        // Cancel all active assignments for this job
        var assignments = await _assignmentRepository.GetByJobIdAsync(request.JobId, cancellationToken);
        var activeAssignments = assignments
            .Where(a => a.Status != AssignmentEntityStatus.Cancelled && a.Status != AssignmentEntityStatus.Completed)
            .ToList();

        foreach (var assignment in activeAssignments)
        {
            assignment.Cancel(reason);
            await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
        }

        // Publish JobCancelled event via SignalR
        // MVP uses default region - can be enhanced to derive from job location or user context
        const string region = "Default";
        var contractorIds = activeAssignments.Select(a => a.ContractorId.ToString()).ToList();
        await _realtimePublisher.PublishJobCancelledAsync(
            request.JobId.ToString(),
            reason,
            contractorIds,
            region,
            cancellationToken);

        // Run consistency checks for all affected contractors (fire-and-forget, don't block operation)
        _ = Task.Run(async () =>
        {
            try
            {
                var uniqueContractorIds = activeAssignments.Select(a => a.ContractorId).Distinct();
                foreach (var contractorId in uniqueContractorIds)
                {
                    var consistencyResult = await _consistencyChecker.CheckConsistencyAsync(contractorId, cancellationToken);
                    if (!consistencyResult.IsConsistent)
                    {
                        // Log issues but don't fail the operation
                        // In a production system, these could be sent to a monitoring/alerting system
                    }
                }
            }
            catch
            {
                // Silently ignore consistency check errors - they shouldn't block the cancel
            }
        }, cancellationToken);

        return job.ToDto();
    }
}


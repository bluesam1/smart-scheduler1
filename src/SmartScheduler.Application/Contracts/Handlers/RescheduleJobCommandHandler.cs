using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for RescheduleJobCommand.
/// </summary>
public class RescheduleJobCommandHandler : IRequestHandler<RescheduleJobCommand, JobDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAvailabilityRevalidator _availabilityRevalidator;
    private readonly IContractorRepository _contractorRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ICalendarConsistencyChecker _consistencyChecker;

    public RescheduleJobCommandHandler(
        IJobRepository jobRepository,
        IAssignmentRepository assignmentRepository,
        IAvailabilityRevalidator availabilityRevalidator,
        IContractorRepository contractorRepository,
        IRealtimePublisher realtimePublisher,
        ICalendarConsistencyChecker consistencyChecker)
    {
        _jobRepository = jobRepository;
        _assignmentRepository = assignmentRepository;
        _availabilityRevalidator = availabilityRevalidator;
        _contractorRepository = contractorRepository;
        _realtimePublisher = realtimePublisher;
        _consistencyChecker = consistencyChecker;
    }

    public async Task<JobDto> Handle(
        RescheduleJobCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Validate new time slot
        if (req.StartUtc >= req.EndUtc)
        {
            throw new ArgumentException("Start time must be before end time.", nameof(req.StartUtc));
        }

        // Validate job exists
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");
        }

        // Cannot reschedule completed or canceled jobs
        if (job.Status == JobStatus.Completed)
        {
            throw new InvalidOperationException("Cannot reschedule a completed job.");
        }

        if (job.Status == JobStatus.Canceled)
        {
            throw new InvalidOperationException("Cannot reschedule a canceled job.");
        }

        // Get all active assignments for this job
        var assignments = await _assignmentRepository.GetByJobIdAsync(request.JobId, cancellationToken);
        var activeAssignments = assignments
            .Where(a => a.Status != AssignmentEntityStatus.Cancelled && a.Status != AssignmentEntityStatus.Completed)
            .ToList();

        // Validate feasibility for all assigned contractors
        var newServiceWindow = new TimeWindow(req.StartUtc, req.EndUtc);
        var jobDurationMinutes = job.Duration;

        foreach (var assignment in activeAssignments)
        {
            // Calculate new assignment time slot based on job duration
            // For simplicity, we'll use the new service window start/end
            // In a more sophisticated implementation, we might adjust based on contractor availability
            var newStartUtc = req.StartUtc;
            var newEndUtc = req.StartUtc.AddMinutes(jobDurationMinutes);

            // Validate contractor availability for new time slot
            var validationResult = await _availabilityRevalidator.ValidateAvailabilityAsync(
                assignment.ContractorId,
                request.JobId,
                newStartUtc,
                newEndUtc,
                jobDurationMinutes,
                job.Timezone,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                // Get contractor name for better error message
                var contractor = await _contractorRepository.GetByIdAsync(assignment.ContractorId, cancellationToken);
                var contractorName = contractor?.Name ?? assignment.ContractorId.ToString();
                
                throw new InvalidOperationException(
                    $"Cannot reschedule job: Contractor {contractorName} is not available for the new time slot. " +
                    $"{validationResult.ErrorMessage}");
            }
        }

        // Check for conflicts with other assignments
        foreach (var assignment in activeAssignments)
        {
            var newStartUtc = req.StartUtc;
            var newEndUtc = req.StartUtc.AddMinutes(jobDurationMinutes);

            // Get all assignments for this contractor in the new time range (excluding this job's assignments)
            var contractorAssignments = await _assignmentRepository.GetByContractorIdAndTimeRangeAsync(
                assignment.ContractorId,
                newStartUtc,
                newEndUtc,
                cancellationToken);

            var conflictingAssignments = contractorAssignments
                .Where(a => a.JobId != request.JobId && 
                           a.Status != AssignmentEntityStatus.Cancelled &&
                           a.Status != AssignmentEntityStatus.Completed)
                .ToList();

            if (conflictingAssignments.Any())
            {
                var contractor = await _contractorRepository.GetByIdAsync(assignment.ContractorId, cancellationToken);
                var contractorName = contractor?.Name ?? assignment.ContractorId.ToString();
                
                throw new InvalidOperationException(
                    $"Cannot reschedule job: Contractor {contractorName} has conflicting assignments at the new time slot.");
            }
        }

        // Store previous service window for event publishing
        var previousStartUtc = job.ServiceWindow.Start;
        var previousEndUtc = job.ServiceWindow.End;

        // Update job service window (this raises JobRescheduled domain event)
        job.Reschedule(newServiceWindow);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        // Update all active assignments with new time slots
        foreach (var assignment in activeAssignments)
        {
            var newStartUtc = req.StartUtc;
            var newEndUtc = req.StartUtc.AddMinutes(jobDurationMinutes);
            
            assignment.UpdateTimeSlot(newStartUtc, newEndUtc);
            await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
        }

        // Publish JobRescheduled event via SignalR
        // MVP uses default region - can be enhanced to derive from job location or user context
        const string region = "Default";
        var contractorIds = activeAssignments.Select(a => a.ContractorId.ToString()).ToList();
        await _realtimePublisher.PublishJobRescheduledAsync(
            request.JobId.ToString(),
            previousStartUtc,
            previousEndUtc,
            req.StartUtc,
            req.EndUtc,
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
                // Silently ignore consistency check errors - they shouldn't block the reschedule
            }
        }, cancellationToken);

        return job.ToDto();
    }
}


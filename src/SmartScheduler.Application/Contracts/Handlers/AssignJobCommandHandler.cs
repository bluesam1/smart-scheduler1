using MediatR;
using SmartScheduler.Application.Contracts.Commands;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Mapping;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Realtime.Services;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for AssignJobCommand.
/// </summary>
public class AssignJobCommandHandler : IRequestHandler<AssignJobCommand, AssignmentDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAvailabilityRevalidator _availabilityRevalidator;
    private readonly IContractorRepository _contractorRepository;
    private readonly IRealtimePublisher _realtimePublisher;

    public AssignJobCommandHandler(
        IJobRepository jobRepository,
        IAssignmentRepository assignmentRepository,
        IAvailabilityRevalidator availabilityRevalidator,
        IContractorRepository contractorRepository,
        IRealtimePublisher realtimePublisher)
    {
        _jobRepository = jobRepository;
        _assignmentRepository = assignmentRepository;
        _availabilityRevalidator = availabilityRevalidator;
        _contractorRepository = contractorRepository;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<AssignmentDto> Handle(
        AssignJobCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Validate job exists
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");
        }

        // Validate contractor exists
        var contractor = await _contractorRepository.GetByIdAsync(req.ContractorId, cancellationToken);
        if (contractor == null)
        {
            throw new KeyNotFoundException($"Contractor with ID {req.ContractorId} not found.");
        }

        // Check for existing assignment (idempotency)
        var existingAssignments = await _assignmentRepository.GetByJobIdAsync(request.JobId, cancellationToken);
        var existingAssignment = existingAssignments.FirstOrDefault(a => 
            a.ContractorId == req.ContractorId &&
            a.StartUtc == req.StartUtc &&
            a.EndUtc == req.EndUtc &&
            a.Status != AssignmentEntityStatus.Cancelled);

        if (existingAssignment != null)
        {
            // Return existing assignment (idempotent)
            return existingAssignment.ToDto();
        }

        // Re-validate availability
        var validationResult = await _availabilityRevalidator.ValidateAvailabilityAsync(
            req.ContractorId,
            request.JobId,
            req.StartUtc,
            req.EndUtc,
            job.Duration,
            job.Timezone,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(validationResult.ErrorMessage ?? "Contractor is not available for this time slot.");
        }

        // Parse assignment source
        if (!Enum.TryParse<AssignmentSource>(req.Source, ignoreCase: true, out var source))
        {
            source = AssignmentSource.Auto;
        }

        // Create assignment entity
        var assignment = new Assignment(
            Guid.NewGuid(),
            request.JobId,
            req.ContractorId,
            req.StartUtc,
            req.EndUtc,
            source,
            req.AuditId);

        // Update job status to Assigned
        if (job.Status == JobStatus.Created)
        {
            job.UpdateStatus(JobStatus.Assigned);
            await _jobRepository.UpdateAsync(job, cancellationToken);
        }

        // Also add to job's AssignedContractors collection (for backward compatibility)
        job.AssignContractor(req.ContractorId, req.StartUtc, req.EndUtc);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        // Save assignment
        await _assignmentRepository.AddAsync(assignment, cancellationToken);

        // Publish JobAssigned event via SignalR
        // MVP uses default region - can be enhanced to derive from job location or user context
        // Publisher handles errors gracefully and won't throw, so this won't block assignment
        const string region = "Default";
        await _realtimePublisher.PublishJobAssignedAsync(
            request.JobId.ToString(),
            req.ContractorId.ToString(),
            assignment.Id.ToString(),
            req.StartUtc,
            req.EndUtc,
            region,
            source.ToString(),
            req.AuditId?.ToString() ?? string.Empty,
            cancellationToken);

        return assignment.ToDto();
    }
}


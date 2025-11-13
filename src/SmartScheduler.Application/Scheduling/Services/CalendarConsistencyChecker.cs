using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Scheduling.Services;

/// <summary>
/// Implementation of calendar consistency checker.
/// </summary>
public class CalendarConsistencyChecker : ICalendarConsistencyChecker
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IContractorRepository _contractorRepository;
    private readonly ILogger<CalendarConsistencyChecker> _logger;

    public CalendarConsistencyChecker(
        IAssignmentRepository assignmentRepository,
        IContractorRepository contractorRepository,
        ILogger<CalendarConsistencyChecker> logger)
    {
        _assignmentRepository = assignmentRepository;
        _contractorRepository = contractorRepository;
        _logger = logger;
    }

    public async Task<CalendarConsistencyResult> CheckConsistencyAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        // Get all active assignments for the contractor
        var assignments = await _assignmentRepository.GetByContractorIdAsync(contractorId, cancellationToken);
        var activeAssignments = assignments
            .Where(a => a.Status != AssignmentEntityStatus.Cancelled && 
                       a.Status != AssignmentEntityStatus.Completed)
            .OrderBy(a => a.StartUtc)
            .ToList();

        var issues = new List<ConsistencyIssue>();

        // Check for overlaps
        for (int i = 0; i < activeAssignments.Count; i++)
        {
            for (int j = i + 1; j < activeAssignments.Count; j++)
            {
                var assignment1 = activeAssignments[i];
                var assignment2 = activeAssignments[j];

                // Check if assignments overlap
                if (assignment1.StartUtc < assignment2.EndUtc && assignment2.StartUtc < assignment1.EndUtc)
                {
                    issues.Add(new ConsistencyIssue
                    {
                        Type = ConsistencyIssueType.Overlap,
                        Description = $"Assignments overlap: {assignment1.Id} ({assignment1.StartUtc:O} - {assignment1.EndUtc:O}) and {assignment2.Id} ({assignment2.StartUtc:O} - {assignment2.EndUtc:O})",
                        AssignmentId1 = assignment1.Id,
                        AssignmentId2 = assignment2.Id
                    });
                }
            }
        }

        // Check for invalid gaps (missing travel buffers)
        // Note: This is a simplified check - a full implementation would calculate travel times
        // For now, we'll check for gaps that are too small (less than 15 minutes)
        const int minimumGapMinutes = 15;
        
        for (int i = 0; i < activeAssignments.Count - 1; i++)
        {
            var current = activeAssignments[i];
            var next = activeAssignments[i + 1];

            var gapMinutes = (next.StartUtc - current.EndUtc).TotalMinutes;
            
            if (gapMinutes > 0 && gapMinutes < minimumGapMinutes)
            {
                issues.Add(new ConsistencyIssue
                {
                    Type = ConsistencyIssueType.InvalidGap,
                    Description = $"Invalid gap between assignments: {current.Id} and {next.Id}. Gap is {gapMinutes} minutes, minimum is {minimumGapMinutes} minutes.",
                    AssignmentId1 = current.Id,
                    AssignmentId2 = next.Id
                });
            }
        }

        if (issues.Count == 0)
        {
            return CalendarConsistencyResult.Consistent();
        }

        _logger.LogWarning(
            "Found {Count} consistency issues for contractor {ContractorId}",
            issues.Count,
            contractorId);

        return CalendarConsistencyResult.Inconsistent(issues);
    }

    public async Task<CalendarConsistencyCorrectionResult> AttemptCorrectionAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        // For MVP, we'll log issues but not attempt automatic corrections
        // Automatic corrections could be added in the future for simple cases
        var consistencyResult = await CheckConsistencyAsync(contractorId, cancellationToken);

        if (consistencyResult.IsConsistent)
        {
            return CalendarConsistencyCorrectionResult.NoCorrections();
        }

        _logger.LogInformation(
            "Consistency issues found for contractor {ContractorId}, but automatic correction is not implemented in MVP",
            contractorId);

        // Return remaining issues without corrections
        return CalendarConsistencyCorrectionResult.WithCorrections(
            Array.Empty<string>(),
            consistencyResult.Issues);
    }
}


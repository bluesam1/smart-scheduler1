using MediatR;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Application.Contracts.Queries;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using System.Text.Json;

namespace SmartScheduler.Application.Contracts.Handlers;

/// <summary>
/// Handler for GetActivitiesQuery.
/// Transforms EventLog records to Activity DTOs.
/// </summary>
public class GetActivitiesQueryHandler : IRequestHandler<GetActivitiesQuery, IReadOnlyList<ActivityDto>>
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<GetActivitiesQueryHandler> _logger;

    public GetActivitiesQueryHandler(
        IEventLogRepository eventLogRepository,
        ILogger<GetActivitiesQueryHandler> logger)
    {
        _eventLogRepository = eventLogRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ActivityDto>> Handle(
        GetActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        // Validate limit (max 100)
        var limit = Math.Min(request.Limit, 100);
        if (limit <= 0)
            limit = 20;

        // Map activity types to event types
        var eventTypes = request.Types != null && request.Types.Count > 0
            ? MapActivityTypesToEventTypes(request.Types)
            : null;

        // Get recent events
        var events = await _eventLogRepository.GetRecentAsync(limit, eventTypes, cancellationToken);

        // Transform to activities
        var activities = events.Select(e => TransformToActivity(e)).ToList();

        return activities;
    }

    private static IReadOnlyList<string>? MapActivityTypesToEventTypes(IReadOnlyList<string> activityTypes)
    {
        var mapping = new Dictionary<string, string>
        {
            { "assignment", "JobAssigned" },
            { "completion", "JobCompleted" },
            { "cancellation", "JobCancelled" },
            { "contractor_added", "ContractorCreated" },
            { "job_created", "JobCreated" }
        };

        return activityTypes
            .Where(t => mapping.ContainsKey(t))
            .Select(t => mapping[t])
            .ToList();
    }

    private ActivityDto TransformToActivity(EventLog eventLog)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(eventLog.PayloadJson) ?? new Dictionary<string, object>();

            var (title, description, activityType) = GenerateActivityText(eventLog.EventType, payload);

            return new ActivityDto
            {
                Id = eventLog.Id.ToString(),
                Type = activityType,
                Title = title,
                Description = description,
                Timestamp = eventLog.CreatedAt,
                Metadata = payload
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to transform event log {EventId} to activity", eventLog.Id);
            
            // Return a basic activity if transformation fails
            return new ActivityDto
            {
                Id = eventLog.Id.ToString(),
                Type = "unknown",
                Title = $"Event: {eventLog.EventType}",
                Description = "Unable to parse event details",
                Timestamp = eventLog.CreatedAt,
                Metadata = new Dictionary<string, object>()
            };
        }
    }

    private static (string Title, string Description, string ActivityType) GenerateActivityText(
        string eventType,
        Dictionary<string, object> payload)
    {
        return eventType switch
        {
            "JobAssigned" => (
                "Job Assigned",
                GenerateJobAssignedDescription(payload),
                "assignment"
            ),
            "JobCompleted" => (
                "Job Completed",
                GenerateJobCompletedDescription(payload),
                "completion"
            ),
            "JobCancelled" => (
                "Job Cancelled",
                GenerateJobCancelledDescription(payload),
                "cancellation"
            ),
            "JobCreated" => (
                "Job Created",
                GenerateJobCreatedDescription(payload),
                "job_created"
            ),
            "ContractorCreated" => (
                "Contractor Added",
                GenerateContractorCreatedDescription(payload),
                "contractor_added"
            ),
            _ => (
                $"Event: {eventType}",
                "System event occurred",
                "unknown"
            )
        };
    }

    private static string GenerateJobAssignedDescription(Dictionary<string, object> payload)
    {
        var jobId = payload.GetValueOrDefault("JobId")?.ToString() ?? "Unknown";
        var contractorId = payload.GetValueOrDefault("ContractorId")?.ToString() ?? "Unknown";
        return $"Job {jobId.Substring(0, 8)}... assigned to contractor {contractorId.Substring(0, 8)}...";
    }

    private static string GenerateJobCompletedDescription(Dictionary<string, object> payload)
    {
        var jobId = payload.GetValueOrDefault("JobId")?.ToString() ?? "Unknown";
        return $"Job {jobId.Substring(0, 8)}... has been completed";
    }

    private static string GenerateJobCancelledDescription(Dictionary<string, object> payload)
    {
        var jobId = payload.GetValueOrDefault("JobId")?.ToString() ?? "Unknown";
        var reason = payload.GetValueOrDefault("Reason")?.ToString();
        return reason != null
            ? $"Job {jobId.Substring(0, 8)}... was cancelled: {reason}"
            : $"Job {jobId.Substring(0, 8)}... was cancelled";
    }

    private static string GenerateJobCreatedDescription(Dictionary<string, object> payload)
    {
        var jobId = payload.GetValueOrDefault("JobId")?.ToString() ?? "Unknown";
        var jobType = payload.GetValueOrDefault("JobType")?.ToString() ?? "job";
        var address = payload.GetValueOrDefault("Address")?.ToString() ?? "location";
        return $"New {jobType} created at {address}";
    }

    private static string GenerateContractorCreatedDescription(Dictionary<string, object> payload)
    {
        var contractorId = payload.GetValueOrDefault("ContractorId")?.ToString() ?? "Unknown";
        var name = payload.GetValueOrDefault("Name")?.ToString() ?? "contractor";
        return $"New contractor {name} added to the system";
    }
}


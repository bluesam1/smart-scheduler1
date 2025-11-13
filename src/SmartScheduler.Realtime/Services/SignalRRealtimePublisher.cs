using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Realtime.Hubs;
using System.Text.Json;

namespace SmartScheduler.Realtime.Services;

/// <summary>
/// SignalR implementation of IRealtimePublisher for publishing real-time events.
/// Also persists events to EventLog for audit trail.
/// </summary>
public class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<RecommendationsHub> _hubContext;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<SignalRRealtimePublisher> _logger;

    public SignalRRealtimePublisher(
        IHubContext<RecommendationsHub> hubContext,
        IEventLogRepository eventLogRepository,
        ILogger<SignalRRealtimePublisher> logger)
    {
        _hubContext = hubContext;
        _eventLogRepository = eventLogRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishRecommendationReadyAsync(
        string jobId,
        string requestId,
        string region,
        int configVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = $"dispatch/{region}";
            var payload = new
            {
                type = "RecommendationReady",
                jobId,
                requestId,
                region,
                configVersion,
                generatedAt = DateTime.UtcNow.ToString("O") // ISO8601 format
            };

            await _hubContext.Clients.Group(groupName).SendAsync("RecommendationReady", payload, cancellationToken);
            
            // Persist to EventLog
            await PersistEventAsync("RecommendationReady", payload, new[] { groupName }, cancellationToken);
            
            _logger.LogInformation(
                "Published RecommendationReady event for job {JobId} to group {GroupName}",
                jobId,
                groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish RecommendationReady event for job {JobId} to region {Region}",
                jobId,
                region);
            // Don't throw - real-time events should not block the main flow
        }
    }

    /// <inheritdoc />
    public async Task PublishJobAssignedAsync(
        string jobId,
        string contractorId,
        string assignmentId,
        DateTime startUtc,
        DateTime endUtc,
        string region,
        string source,
        string auditId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                type = "JobAssigned",
                jobId,
                contractorId,
                assignmentId,
                startUtc = startUtc.ToString("O"), // ISO8601 format
                endUtc = endUtc.ToString("O"), // ISO8601 format
                region,
                source,
                auditId
            };

            // Publish to both dispatcher group and contractor group
            var dispatchGroupName = $"dispatch/{region}";
            var contractorGroupName = $"contractor/{contractorId}";

            // Send to dispatcher group
            await _hubContext.Clients.Group(dispatchGroupName).SendAsync("JobAssigned", payload, cancellationToken);
            
            // Send to contractor group
            await _hubContext.Clients.Group(contractorGroupName).SendAsync("JobAssigned", payload, cancellationToken);

            // Persist to EventLog
            await PersistEventAsync("JobAssigned", payload, new[] { dispatchGroupName, contractorGroupName }, cancellationToken);

            _logger.LogInformation(
                "Published JobAssigned event for job {JobId} to groups {DispatchGroup} and {ContractorGroup}",
                jobId,
                dispatchGroupName,
                contractorGroupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish JobAssigned event for job {JobId} to contractor {ContractorId}",
                jobId,
                contractorId);
            // Don't throw - real-time events should not block the main flow
        }
    }

    /// <inheritdoc />
    public async Task PublishJobRescheduledAsync(
        string jobId,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        DateTime newStartUtc,
        DateTime newEndUtc,
        IReadOnlyList<string> contractorIds,
        string region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                type = "JobRescheduled",
                jobId,
                previousStartUtc = previousStartUtc.ToString("O"), // ISO8601 format
                previousEndUtc = previousEndUtc.ToString("O"), // ISO8601 format
                newStartUtc = newStartUtc.ToString("O"), // ISO8601 format
                newEndUtc = newEndUtc.ToString("O"), // ISO8601 format
                region
            };

            var dispatchGroupName = $"dispatch/{region}";

            // Send to dispatcher group
            await _hubContext.Clients.Group(dispatchGroupName).SendAsync("JobRescheduled", payload, cancellationToken);

            // Build list of all groups for EventLog
            var publishedGroups = new List<string> { dispatchGroupName };
            
            // Send to each contractor group
            foreach (var contractorId in contractorIds)
            {
                var contractorGroupName = $"contractor/{contractorId}";
                await _hubContext.Clients.Group(contractorGroupName).SendAsync("JobRescheduled", payload, cancellationToken);
                publishedGroups.Add(contractorGroupName);
            }

            // Persist to EventLog
            await PersistEventAsync("JobRescheduled", payload, publishedGroups, cancellationToken);

            _logger.LogInformation(
                "Published JobRescheduled event for job {JobId} to group {DispatchGroup} and {Count} contractor groups",
                jobId,
                dispatchGroupName,
                contractorIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish JobRescheduled event for job {JobId}",
                jobId);
            // Don't throw - real-time events should not block the main flow
        }
    }

    /// <inheritdoc />
    public async Task PublishJobCancelledAsync(
        string jobId,
        string reason,
        IReadOnlyList<string> contractorIds,
        string region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                type = "JobCancelled",
                jobId,
                reason,
                region
            };

            var dispatchGroupName = $"dispatch/{region}";

            // Send to dispatcher group
            await _hubContext.Clients.Group(dispatchGroupName).SendAsync("JobCancelled", payload, cancellationToken);

            // Build list of all groups for EventLog
            var publishedGroups = new List<string> { dispatchGroupName };
            
            // Send to each contractor group
            foreach (var contractorId in contractorIds)
            {
                var contractorGroupName = $"contractor/{contractorId}";
                await _hubContext.Clients.Group(contractorGroupName).SendAsync("JobCancelled", payload, cancellationToken);
                publishedGroups.Add(contractorGroupName);
            }

            // Persist to EventLog
            await PersistEventAsync("JobCancelled", payload, publishedGroups, cancellationToken);

            _logger.LogInformation(
                "Published JobCancelled event for job {JobId} to group {DispatchGroup} and {Count} contractor groups",
                jobId,
                dispatchGroupName,
                contractorIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish JobCancelled event for job {JobId}",
                jobId);
            // Don't throw - real-time events should not block the main flow
        }
    }

    /// <summary>
    /// Persists an event to EventLog for audit trail.
    /// </summary>
    private async Task PersistEventAsync(
        string eventType,
        object payload,
        IReadOnlyList<string> publishedTo,
        CancellationToken cancellationToken)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var eventLog = new EventLog(
                Guid.NewGuid(),
                eventType,
                payloadJson,
                DateTime.UtcNow,
                publishedTo);

            await _eventLogRepository.AddAsync(eventLog, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't throw - EventLog persistence should not block event publishing
            _logger.LogWarning(
                ex,
                "Failed to persist event {EventType} to EventLog",
                eventType);
        }
    }
}


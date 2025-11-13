namespace SmartScheduler.Realtime.Services;

/// <summary>
/// Interface for publishing real-time events via SignalR.
/// </summary>
public interface IRealtimePublisher
{
    /// <summary>
    /// Publishes a RecommendationReady event to dispatcher groups.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="requestId">The request identifier (correlates to HTTP POST /recommendations call)</param>
    /// <param name="region">The region identifier</param>
    /// <param name="configVersion">The configuration version used for recommendations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishRecommendationReadyAsync(
        string jobId,
        string requestId,
        string region,
        int configVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a JobAssigned event to both dispatcher and contractor groups.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="contractorId">The contractor identifier</param>
    /// <param name="assignmentId">The assignment identifier</param>
    /// <param name="startUtc">The assignment start time (UTC)</param>
    /// <param name="endUtc">The assignment end time (UTC)</param>
    /// <param name="region">The region identifier</param>
    /// <param name="source">The assignment source (auto or manual)</param>
    /// <param name="auditId">The audit recommendation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishJobAssignedAsync(
        string jobId,
        string contractorId,
        string assignmentId,
        DateTime startUtc,
        DateTime endUtc,
        string region,
        string source,
        string auditId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a JobRescheduled event to both dispatcher and contractor groups.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="previousStartUtc">The previous service window start time (UTC)</param>
    /// <param name="previousEndUtc">The previous service window end time (UTC)</param>
    /// <param name="newStartUtc">The new service window start time (UTC)</param>
    /// <param name="newEndUtc">The new service window end time (UTC)</param>
    /// <param name="contractorIds">List of contractor IDs assigned to this job</param>
    /// <param name="region">The region identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishJobRescheduledAsync(
        string jobId,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        DateTime newStartUtc,
        DateTime newEndUtc,
        IReadOnlyList<string> contractorIds,
        string region,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a JobCancelled event to both dispatcher and contractor groups.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="reason">The cancellation reason</param>
    /// <param name="contractorIds">List of contractor IDs assigned to this job</param>
    /// <param name="region">The region identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishJobCancelledAsync(
        string jobId,
        string reason,
        IReadOnlyList<string> contractorIds,
        string region,
        CancellationToken cancellationToken = default);
}


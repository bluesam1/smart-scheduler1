namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for processing multiple distance/ETA requests in batches efficiently.
/// </summary>
public interface IBatchDistanceProcessor
{
    /// <summary>
    /// Processes multiple distance/ETA requests in optimized batches.
    /// </summary>
    /// <param name="requests">List of distance/ETA requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping request index to result (distance in meters, ETA in minutes)</returns>
    Task<Dictionary<int, (double? DistanceMeters, int? EtaMinutes)>> ProcessBatchAsync(
        IReadOnlyList<DistanceRequest> requests,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a distance/ETA calculation request.
/// </summary>
public class DistanceRequest
{
    public double OriginLat { get; init; }
    public double OriginLng { get; init; }
    public double DestinationLat { get; init; }
    public double DestinationLng { get; init; }
}



namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for distance and ETA calculations with fallback support.
/// Falls back to Haversine when ORS is unavailable.
/// </summary>
public interface IDistanceCalculationService
{
    /// <summary>
    /// Calculates distance in meters between two points.
    /// Falls back to Haversine if ORS is unavailable.
    /// </summary>
    Task<DistanceResult> CalculateDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates ETA in minutes between two points.
    /// Falls back to Haversine-based estimate if ORS is unavailable.
    /// </summary>
    Task<EtaResult> CalculateEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of distance calculation with metadata.
/// </summary>
public class DistanceResult
{
    public double? DistanceMeters { get; init; }
    public bool IsDegraded { get; init; }
    public string? Source { get; init; } // "ORS" or "Haversine"
}

/// <summary>
/// Result of ETA calculation with metadata.
/// </summary>
public class EtaResult
{
    public int? EtaMinutes { get; init; }
    public bool IsDegraded { get; init; }
    public string? Source { get; init; } // "ORS" or "Haversine"
}





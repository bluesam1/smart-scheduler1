namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service interface for distance and ETA calculations with caching.
/// </summary>
public interface IDistanceService
{
    /// <summary>
    /// Gets the distance in meters between two points (with caching).
    /// </summary>
    Task<double?> GetDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the ETA in minutes between two points (with caching).
    /// </summary>
    Task<int?> GetEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);
}



namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Client interface for OpenRouteService API v2.
/// Provides distance and ETA calculations between locations.
/// </summary>
public interface IOpenRouteServiceClient
{
    /// <summary>
    /// Calculates the distance in meters between two points.
    /// </summary>
    /// <param name="originLat">Origin latitude</param>
    /// <param name="originLng">Origin longitude</param>
    /// <param name="destinationLat">Destination latitude</param>
    /// <param name="destinationLng">Destination longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Distance in meters, or null if calculation failed</returns>
    Task<double?> CalculateDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the estimated travel time (ETA) in minutes between two points.
    /// </summary>
    /// <param name="originLat">Origin latitude</param>
    /// <param name="originLng">Origin longitude</param>
    /// <param name="destinationLat">Destination latitude</param>
    /// <param name="destinationLng">Destination longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETA in minutes, or null if calculation failed</returns>
    Task<int?> CalculateEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates distance and ETA matrix for multiple origin-destination pairs.
    /// </summary>
    /// <param name="origins">List of origin coordinates (lat, lng)</param>
    /// <param name="destinations">List of destination coordinates (lat, lng)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matrix with distances (meters) and ETAs (minutes), or null if calculation failed</returns>
    Task<DistanceEtaMatrix?> CalculateMatrixAsync(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a distance and ETA matrix result.
/// </summary>
public class DistanceEtaMatrix
{
    /// <summary>
    /// Distance matrix in meters. [originIndex][destinationIndex]
    /// </summary>
    public double[][] Distances { get; init; } = Array.Empty<double[]>();

    /// <summary>
    /// ETA matrix in minutes. [originIndex][destinationIndex]
    /// </summary>
    public int[][] Etas { get; init; } = Array.Empty<int[]>();
}



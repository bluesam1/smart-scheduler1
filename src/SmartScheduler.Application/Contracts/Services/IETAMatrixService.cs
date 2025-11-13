namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for calculating ETAs for multiple contractor-job pairs in batch.
/// </summary>
public interface IETAMatrixService
{
    /// <summary>
    /// Calculates ETAs for multiple origin-destination pairs efficiently.
    /// </summary>
    /// <param name="origins">List of origin coordinates (lat, lng)</param>
    /// <param name="destinations">List of destination coordinates (lat, lng)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping (originIndex, destinationIndex) to ETA in minutes, or null if calculation failed</returns>
    Task<Dictionary<(int OriginIndex, int DestinationIndex), int>?> CalculateETAsAsync(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default);
}


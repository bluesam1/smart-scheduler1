namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for looking up timezone from geographic coordinates.
/// </summary>
public interface ITimezoneService
{
    /// <summary>
    /// Gets the IANA timezone identifier for a given latitude and longitude.
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IANA timezone identifier (e.g., "America/New_York")</returns>
    Task<string> GetTimezoneAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}


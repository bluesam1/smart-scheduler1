using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.Services;

namespace SmartScheduler.Infrastructure.Services;

/// <summary>
/// Cached wrapper for distance and ETA calculations.
/// Caches results to reduce API calls and improve performance.
/// </summary>
public class CachedDistanceService : SmartScheduler.Application.Contracts.Services.IDistanceService
{
    private readonly IOpenRouteServiceClient _orsClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedDistanceService> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(15); // 15 minute TTL for traffic variations

    public CachedDistanceService(
        IOpenRouteServiceClient orsClient,
        IMemoryCache cache,
        ILogger<CachedDistanceService> logger)
    {
        _orsClient = orsClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<double?> GetDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("distance", originLat, originLng, destinationLat, destinationLng);
        
        if (_cache.TryGetValue<double?>(cacheKey, out var cachedDistance))
        {
            _logger.LogDebug("Distance retrieved from cache");
            return cachedDistance;
        }

        var distance = await _orsClient.CalculateDistanceAsync(
            originLat, originLng, destinationLat, destinationLng, cancellationToken);

        if (distance.HasValue)
        {
            _cache.Set(cacheKey, distance, _cacheTtl);
        }

        return distance;
    }

    /// <inheritdoc />
    public async Task<int?> GetEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("eta", originLat, originLng, destinationLat, destinationLng);
        
        if (_cache.TryGetValue<int?>(cacheKey, out var cachedEta))
        {
            _logger.LogDebug("ETA retrieved from cache");
            return cachedEta;
        }

        var eta = await _orsClient.CalculateEtaAsync(
            originLat, originLng, destinationLat, destinationLng, cancellationToken);

        if (eta.HasValue)
        {
            _cache.Set(cacheKey, eta, _cacheTtl);
        }

        return eta;
    }

    /// <summary>
    /// Generates a cache key from coordinates.
    /// Rounds coordinates to 4 decimal places (~11 meters precision) for cache efficiency.
    /// </summary>
    private string GenerateCacheKey(string type, double originLat, double originLng, double destinationLat, double destinationLng)
    {
        // Round to 4 decimal places for cache efficiency (avoids cache misses due to minor coordinate differences)
        var roundedOriginLat = Math.Round(originLat, 4);
        var roundedOriginLng = Math.Round(originLng, 4);
        var roundedDestLat = Math.Round(destinationLat, 4);
        var roundedDestLng = Math.Round(destinationLng, 4);

        return $"{type}:{roundedOriginLat},{roundedOriginLng}:{roundedDestLat},{roundedDestLng}";
    }
}


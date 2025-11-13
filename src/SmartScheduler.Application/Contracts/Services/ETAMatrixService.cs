using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for calculating ETAs for multiple contractor-job pairs efficiently.
/// Uses ORS matrix API when available, falls back to individual requests if needed.
/// </summary>
public class ETAMatrixService : IETAMatrixService
{
    private readonly IOpenRouteServiceClient _orsClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ETAMatrixService> _logger;
    private const int MaxBatchSize = 25; // ORS matrix API limit

    public ETAMatrixService(
        IOpenRouteServiceClient orsClient,
        IMemoryCache cache,
        ILogger<ETAMatrixService> logger)
    {
        _orsClient = orsClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Dictionary<(int OriginIndex, int DestinationIndex), int>?> CalculateETAsAsync(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default)
    {
        if (origins.Count == 0 || destinations.Count == 0)
        {
            return new Dictionary<(int, int), int>();
        }

        // Check cache first
        var cacheKey = GenerateCacheKey(origins, destinations);
        if (_cache.TryGetValue<Dictionary<(int, int), int>>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("ETA matrix retrieved from cache for {OriginCount} origins and {DestinationCount} destinations",
                origins.Count, destinations.Count);
            return cachedResult;
        }

        try
        {
            // Try matrix API first (more efficient for batch requests)
            var matrixResult = await _orsClient.CalculateMatrixAsync(origins, destinations, cancellationToken);
            
            if (matrixResult != null)
            {
                // Convert matrix to dictionary
                var result = new Dictionary<(int, int), int>();
                for (int i = 0; i < matrixResult.Etas.Length; i++)
                {
                    for (int j = 0; j < matrixResult.Etas[i].Length; j++)
                    {
                        result[(i, j)] = matrixResult.Etas[i][j];
                    }
                }

                // Cache the result (15 minute TTL to account for traffic variations)
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
                
                return result;
            }

            // Fallback to individual requests if matrix API fails
            _logger.LogWarning("Matrix API failed, falling back to individual requests");
            return await CalculateETAsFallbackAsync(origins, destinations, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA matrix");
            return await CalculateETAsFallbackAsync(origins, destinations, cancellationToken);
        }
    }

    /// <summary>
    /// Fallback method that calculates ETAs using individual requests.
    /// </summary>
    private async Task<Dictionary<(int, int), int>?> CalculateETAsFallbackAsync(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(int, int), int>();
        var tasks = new List<Task>();

        for (int i = 0; i < origins.Count; i++)
        {
            for (int j = 0; j < destinations.Count; j++)
            {
                var origin = origins[i];
                var destination = destinations[j];
                var originIndex = i;
                var destinationIndex = j;

                tasks.Add(Task.Run(async () =>
                {
                    var eta = await _orsClient.CalculateEtaAsync(
                        origin.Lat, origin.Lng,
                        destination.Lat, destination.Lng,
                        cancellationToken);
                    
                    if (eta.HasValue)
                    {
                        lock (result)
                        {
                            result[(originIndex, destinationIndex)] = eta.Value;
                        }
                    }
                }, cancellationToken));
            }
        }

        await Task.WhenAll(tasks);
        return result;
    }

    /// <summary>
    /// Generates a cache key from origin and destination coordinates.
    /// </summary>
    private string GenerateCacheKey(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations)
    {
        // Round coordinates to 4 decimal places (~11 meters precision) for cache efficiency
        var originHashes = origins.Select(o => $"{Math.Round(o.Lat, 4)},{Math.Round(o.Lng, 4)}");
        var destHashes = destinations.Select(d => $"{Math.Round(d.Lat, 4)},{Math.Round(d.Lng, 4)}");
        
        return $"eta_matrix:{string.Join("|", originHashes)}:{string.Join("|", destHashes)}";
    }
}



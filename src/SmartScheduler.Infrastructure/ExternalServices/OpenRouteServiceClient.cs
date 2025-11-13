using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using SmartScheduler.Application.Contracts.Services;

namespace SmartScheduler.Infrastructure.ExternalServices;

/// <summary>
/// Client for OpenRouteService API v2.
/// Handles distance and ETA calculations with resilience policies.
/// </summary>
public class OpenRouteServiceClient : IOpenRouteServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouteServiceClient> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public OpenRouteServiceClient(
        HttpClient httpClient,
        ILogger<OpenRouteServiceClient> logger,
        string apiKey,
        string baseUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <inheritdoc />
    public async Task<double?> CalculateDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use directions API which provides both distance and duration
            var url = $"{_baseUrl}/v2/directions/driving-car?api_key={Uri.EscapeDataString(_apiKey)}";
            var requestBody = new
            {
                coordinates = new[]
                {
                    new[] { originLng, originLat },
                    new[] { destinationLng, destinationLat }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("ORS API returned status {StatusCode} for distance calculation: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<DirectionsResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (result?.Routes == null || result.Routes.Length == 0)
            {
                _logger.LogWarning("ORS API returned no routes for distance calculation");
                return null;
            }

            // Distance is in meters
            return result.Routes[0].Summary?.Distance;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open for ORS API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance via ORS API");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<int?> CalculateEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/v2/directions/driving-car?api_key={Uri.EscapeDataString(_apiKey)}";
            var requestBody = new
            {
                coordinates = new[]
                {
                    new[] { originLng, originLat },
                    new[] { destinationLng, destinationLat }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("ORS API returned status {StatusCode} for ETA calculation: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<DirectionsResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (result?.Routes == null || result.Routes.Length == 0)
            {
                _logger.LogWarning("ORS API returned no routes for ETA calculation");
                return null;
            }

            // Duration is in seconds, convert to minutes
            var durationSeconds = result.Routes[0].Summary?.Duration ?? 0;
            return (int)Math.Ceiling(durationSeconds / 60.0);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open for ORS API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA via ORS API");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DistanceEtaMatrix?> CalculateMatrixAsync(
        IReadOnlyList<(double Lat, double Lng)> origins,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default)
    {
        if (origins.Count == 0 || destinations.Count == 0)
        {
            return new DistanceEtaMatrix
            {
                Distances = Array.Empty<double[]>(),
                Etas = Array.Empty<int[]>()
            };
        }

        try
        {
            var url = $"{_baseUrl}/v2/matrix/driving-car?api_key={Uri.EscapeDataString(_apiKey)}";
            
            // ORS matrix API expects coordinates as [lng, lat] arrays
            var locations = origins.Concat(destinations)
                .Select(loc => new[] { loc.Lng, loc.Lat })
                .ToArray();

            var requestBody = new
            {
                locations = locations,
                sources = Enumerable.Range(0, origins.Count).ToArray(),
                destinations = Enumerable.Range(origins.Count, destinations.Count).ToArray()
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("ORS Matrix API returned status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MatrixResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (result?.Distances == null || result.Durations == null)
            {
                _logger.LogWarning("ORS Matrix API returned invalid response");
                return null;
            }

            // Convert distances (meters) and durations (seconds to minutes)
            var distances = result.Distances.Select(row => 
                row.Select(d => d).ToArray()
            ).ToArray();

            var etas = result.Durations.Select(row => 
                row.Select(d => (int)Math.Ceiling(d / 60.0)).ToArray()
            ).ToArray();

            return new DistanceEtaMatrix
            {
                Distances = distances,
                Etas = etas
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open for ORS Matrix API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating matrix via ORS API");
            return null;
        }
    }

    /// <summary>
    /// Response from ORS Directions API.
    /// </summary>
    private class DirectionsResponse
    {
        public Route[]? Routes { get; set; }
    }

    /// <summary>
    /// Route from ORS Directions API.
    /// </summary>
    private class Route
    {
        public RouteSummary? Summary { get; set; }
    }

    /// <summary>
    /// Route summary with distance and duration.
    /// </summary>
    private class RouteSummary
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
    }

    /// <summary>
    /// Response from ORS Matrix API.
    /// </summary>
    private class MatrixResponse
    {
        public double[][]? Distances { get; set; }
        public double[][]? Durations { get; set; }
    }
}


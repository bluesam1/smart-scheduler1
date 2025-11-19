using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SmartScheduler.Infrastructure.ExternalServices;

/// <summary>
/// Client for Google Places API.
/// Handles Place Details API calls with caching.
/// </summary>
public class GooglePlacesClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GooglePlacesClient> _logger;
    private readonly string _apiKey;

    public GooglePlacesClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<GooglePlacesClient> logger,
        string apiKey)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Gets Place Details from Google Places API by place_id.
    /// Results are cached by place_id.
    /// </summary>
    public async Task<PlaceDetailsResponse?> GetPlaceDetailsAsync(
        string placeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            return null;
        }

        // Check cache first
        var cacheKey = $"google_places_{placeId}";
        if (_cache.TryGetValue<PlaceDetailsResponse>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Place Details retrieved from cache for place_id: {PlaceId}", placeId);
            return cachedResult;
        }

        try
        {
            var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=address_components,formatted_address,geometry,place_id&key={Uri.EscapeDataString(_apiKey)}";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PlaceDetailsApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK" || result.Result == null)
            {
                _logger.LogWarning("Google Places API returned status: {Status} for place_id: {PlaceId}", result?.Status, placeId);
                return null;
            }

            // Cache the result (Place Details don't change, so cache indefinitely)
            _cache.Set(cacheKey, result.Result, TimeSpan.FromDays(365));

            return result.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Places API for place_id: {PlaceId}", placeId);
            return null;
        }
    }

    /// <summary>
    /// Geocodes an address to get coordinates and structured address.
    /// Used as fallback when place_id is not available.
    /// </summary>
    public async Task<GeocodeResponse?> GeocodeAddressAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        // Check cache
        var cacheKey = $"google_geocode_{address.GetHashCode()}";
        if (_cache.TryGetValue<GeocodeResponse>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Geocode result retrieved from cache for address: {Address}", address);
            return cachedResult;
        }

        try
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(_apiKey)}";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GeocodeApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK" || result.Results == null || result.Results.Length == 0)
            {
                _logger.LogWarning("Google Geocoding API returned status: {Status} for address: {Address}", result?.Status, address);
                return null;
            }

            var geocodeResult = result.Results[0];

            // Cache the result
            _cache.Set(cacheKey, geocodeResult, TimeSpan.FromDays(30));

            return geocodeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Geocoding API for address: {Address}", address);
            return null;
        }
    }
}

/// <summary>
/// Response from Google Places Place Details API.
/// </summary>
public class PlaceDetailsApiResponse
{
    public string? Status { get; set; }
    public PlaceDetailsResponse? Result { get; set; }
}

/// <summary>
/// Place Details result from Google Places API.
/// </summary>
public class PlaceDetailsResponse
{
    public AddressComponent[]? AddressComponents { get; set; }
    public string? FormattedAddress { get; set; }
    public Geometry? Geometry { get; set; }
    public string? PlaceId { get; set; }
}

/// <summary>
/// Address component from Google Places API.
/// </summary>
public class AddressComponent
{
    public string? LongName { get; set; }
    public string? ShortName { get; set; }
    public string[]? Types { get; set; }
}

/// <summary>
/// Geometry from Google Places API.
/// </summary>
public class Geometry
{
    public Location? Location { get; set; }
}

/// <summary>
/// Location (lat/lng) from Google Places API.
/// </summary>
public class Location
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

/// <summary>
/// Response from Google Geocoding API.
/// </summary>
public class GeocodeApiResponse
{
    public string? Status { get; set; }
    public GeocodeResponse[]? Results { get; set; }
}

/// <summary>
/// Geocode result from Google Geocoding API.
/// </summary>
public class GeocodeResponse
{
    public AddressComponent[]? AddressComponents { get; set; }
    public string? FormattedAddress { get; set; }
    public Geometry? Geometry { get; set; }
    public string? PlaceId { get; set; }
}





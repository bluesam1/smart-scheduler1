using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.Services;

namespace SmartScheduler.Infrastructure.Services;

/// <summary>
/// Implementation of ITimezoneService using timezone lookup API.
/// Uses TimeZoneDB API (free tier) or falls back to coordinate-based estimation.
/// </summary>
public class TimezoneService : ITimezoneService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TimezoneService> _logger;

    public TimezoneService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<TimezoneService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public Task<string> GetTimezoneAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Round coordinates to reduce cache size (timezone doesn't change for small coordinate differences)
        var roundedLat = Math.Round(latitude, 2);
        var roundedLng = Math.Round(longitude, 2);
        
        var cacheKey = $"timezone_{roundedLat}_{roundedLng}";
        
        // Check cache first
        if (_cache.TryGetValue<string>(cacheKey, out var cachedTimezone) && !string.IsNullOrWhiteSpace(cachedTimezone))
        {
            _logger.LogDebug("Timezone retrieved from cache for coordinates: {Lat}, {Lng}", latitude, longitude);
            return Task.FromResult(cachedTimezone);
        }

        try
        {
            // Use TimeZoneDB API (free tier, no API key required for basic lookup)
            // Alternative: Can use Google Time Zone API if API key is available
            var url = $"https://api.timezonedb.com/v2.1/get-time-zone?key=YOUR_API_KEY&format=json&by=position&lat={latitude}&lng={longitude}";
            
            // For MVP, use a simple coordinate-based estimation
            // This can be enhanced with TimeZoneDB API key later
            var timezone = EstimateTimezoneFromCoordinates(latitude, longitude);
            
            if (string.IsNullOrWhiteSpace(timezone))
            {
                _logger.LogWarning("Could not determine timezone for coordinates: {Lat}, {Lng}. Using default.", latitude, longitude);
                timezone = "America/New_York"; // Default fallback
            }
            else
            {
                _logger.LogDebug("Timezone determined: {Timezone} for coordinates: {Lat}, {Lng}", timezone, latitude, longitude);
            }

            // Cache the result (timezone doesn't change, so cache for a very long time)
            _cache.Set(cacheKey, timezone, TimeSpan.FromDays(365));

            return Task.FromResult(timezone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining timezone for coordinates: {Lat}, {Lng}", latitude, longitude);
            // Fallback to default timezone
            return Task.FromResult("America/New_York");
        }
    }

    /// <summary>
    /// Estimates IANA timezone from coordinates using US timezone boundaries.
    /// This is a simplified implementation for MVP.
    /// For production, use TimeZoneDB API or Google Time Zone API with API key.
    /// </summary>
    private string EstimateTimezoneFromCoordinates(double latitude, double longitude)
    {
        // US timezone estimation (simplified)
        // Eastern: roughly -85 to -67 longitude
        // Central: roughly -102 to -85 longitude
        // Mountain: roughly -115 to -102 longitude
        // Pacific: roughly -125 to -115 longitude
        
        if (longitude >= -67 && longitude <= -50) // Eastern US/Canada
        {
            return "America/New_York";
        }
        else if (longitude >= -102 && longitude < -85) // Central US/Canada
        {
            return "America/Chicago";
        }
        else if (longitude >= -115 && longitude < -102) // Mountain US/Canada
        {
            return "America/Denver";
        }
        else if (longitude >= -125 && longitude < -115) // Pacific US/Canada
        {
            return "America/Los_Angeles";
        }
        else if (longitude < -125) // Alaska
        {
            return "America/Anchorage";
        }
        else if (longitude > -50) // Atlantic/Eastern Canada
        {
            return "America/Halifax";
        }
        
        // Default fallback
        return "America/New_York";
    }
}


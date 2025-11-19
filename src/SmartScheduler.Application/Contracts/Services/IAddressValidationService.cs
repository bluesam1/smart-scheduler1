using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for validating and enriching addresses using Google Places API.
/// </summary>
public interface IAddressValidationService
{
    /// <summary>
    /// Validates and enriches an address using Google Places API.
    /// If placeId is provided, uses Place Details API. Otherwise, uses Autocomplete/Geocoding.
    /// </summary>
    /// <param name="address">Initial address data (may be partial)</param>
    /// <param name="placeId">Optional Google Places place_id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validated and enriched GeoLocation with structured address fields</returns>
    Task<GeoLocation> ValidateAndEnrichAddressAsync(
        GeoLocation address,
        string? placeId = null,
        CancellationToken cancellationToken = default);
}





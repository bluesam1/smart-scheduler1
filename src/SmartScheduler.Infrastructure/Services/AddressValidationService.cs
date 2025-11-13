using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Contracts.Services;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Infrastructure.ExternalServices;

namespace SmartScheduler.Infrastructure.Services;

/// <summary>
/// Implementation of IAddressValidationService using Google Places API.
/// </summary>
public class AddressValidationService : IAddressValidationService
{
    private readonly GooglePlacesClient _googlePlacesClient;
    private readonly ILogger<AddressValidationService> _logger;

    public AddressValidationService(
        GooglePlacesClient googlePlacesClient,
        ILogger<AddressValidationService> logger)
    {
        _googlePlacesClient = googlePlacesClient;
        _logger = logger;
    }

    public async Task<GeoLocation> ValidateAndEnrichAddressAsync(
        GeoLocation address,
        string? placeId = null,
        CancellationToken cancellationToken = default)
    {
        // If place_id is provided, use Place Details API
        if (!string.IsNullOrWhiteSpace(placeId))
        {
            var placeDetails = await _googlePlacesClient.GetPlaceDetailsAsync(placeId, cancellationToken);
            if (placeDetails != null)
            {
                return MapPlaceDetailsToGeoLocation(placeDetails, address);
            }
        }

        // Fallback: Use Geocoding API if we have an address
        var addressString = address.FormattedAddress ?? 
            $"{address.Address}, {address.City}, {address.State} {address.PostalCode}".Trim();
        
        if (!string.IsNullOrWhiteSpace(addressString))
        {
            var geocodeResult = await _googlePlacesClient.GeocodeAddressAsync(addressString, cancellationToken);
            if (geocodeResult != null)
            {
                return MapGeocodeResultToGeoLocation(geocodeResult, address);
            }
        }

        // If all else fails, return the original address
        // This allows manual address entry as fallback
        _logger.LogWarning("Could not validate address via Google Places API. Using provided address as-is.");
        return address;
    }

    private GeoLocation MapPlaceDetailsToGeoLocation(PlaceDetailsResponse placeDetails, GeoLocation originalAddress)
    {
        var addressComponents = placeDetails.AddressComponents ?? Array.Empty<AddressComponent>();
        
        var streetNumber = GetAddressComponent(addressComponents, "street_number");
        var route = GetAddressComponent(addressComponents, "route");
        var streetAddress = string.IsNullOrWhiteSpace(streetNumber) || string.IsNullOrWhiteSpace(route)
            ? originalAddress.Address
            : $"{streetNumber} {route}";
        
        var city = GetAddressComponent(addressComponents, "locality") ?? 
                   GetAddressComponent(addressComponents, "sublocality") ??
                   originalAddress.City;
        
        var state = GetAddressComponent(addressComponents, "administrative_area_level_1", shortName: true) ??
                   originalAddress.State;
        
        var postalCode = GetAddressComponent(addressComponents, "postal_code") ??
                        originalAddress.PostalCode;
        
        var country = GetAddressComponent(addressComponents, "country", shortName: true) ??
                     originalAddress.Country ?? "US";

        var latitude = placeDetails.Geometry?.Location?.Lat ?? originalAddress.Latitude;
        var longitude = placeDetails.Geometry?.Location?.Lng ?? originalAddress.Longitude;

        return new GeoLocation(
            latitude,
            longitude,
            streetAddress,
            city,
            state,
            postalCode,
            country,
            placeDetails.FormattedAddress ?? originalAddress.FormattedAddress,
            placeDetails.PlaceId ?? originalAddress.PlaceId);
    }

    private GeoLocation MapGeocodeResultToGeoLocation(GeocodeResponse geocodeResult, GeoLocation originalAddress)
    {
        var addressComponents = geocodeResult.AddressComponents ?? Array.Empty<AddressComponent>();
        
        var streetNumber = GetAddressComponent(addressComponents, "street_number");
        var route = GetAddressComponent(addressComponents, "route");
        var streetAddress = string.IsNullOrWhiteSpace(streetNumber) || string.IsNullOrWhiteSpace(route)
            ? originalAddress.Address
            : $"{streetNumber} {route}";
        
        var city = GetAddressComponent(addressComponents, "locality") ?? 
                   GetAddressComponent(addressComponents, "sublocality") ??
                   originalAddress.City;
        
        var state = GetAddressComponent(addressComponents, "administrative_area_level_1", shortName: true) ??
                   originalAddress.State;
        
        var postalCode = GetAddressComponent(addressComponents, "postal_code") ??
                        originalAddress.PostalCode;
        
        var country = GetAddressComponent(addressComponents, "country", shortName: true) ??
                     originalAddress.Country ?? "US";

        var latitude = geocodeResult.Geometry?.Location?.Lat ?? originalAddress.Latitude;
        var longitude = geocodeResult.Geometry?.Location?.Lng ?? originalAddress.Longitude;

        return new GeoLocation(
            latitude,
            longitude,
            streetAddress,
            city,
            state,
            postalCode,
            country,
            geocodeResult.FormattedAddress ?? originalAddress.FormattedAddress,
            geocodeResult.PlaceId ?? originalAddress.PlaceId);
    }

    private string? GetAddressComponent(
        AddressComponent[] components,
        string type,
        bool shortName = false)
    {
        var component = components.FirstOrDefault(c => 
            c.Types?.Contains(type, StringComparer.OrdinalIgnoreCase) == true);
        
        return shortName ? component?.ShortName : component?.LongName;
    }
}


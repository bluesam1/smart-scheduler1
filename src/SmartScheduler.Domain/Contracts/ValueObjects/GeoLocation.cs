namespace SmartScheduler.Domain.Contracts.ValueObjects;

/// <summary>
/// Represents a geographic location with coordinates and structured address information.
/// Immutable value object.
/// </summary>
public record GeoLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? FormattedAddress { get; init; }
    public string? PlaceId { get; init; }

    public GeoLocation(
        double latitude,
        double longitude,
        string? address = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        string? country = null,
        string? formattedAddress = null,
        string? placeId = null)
    {
        // Validate coordinates
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country ?? "US";
        FormattedAddress = formattedAddress;
        PlaceId = placeId;
    }

    /// <summary>
    /// Validates that the location has valid coordinates.
    /// </summary>
    public bool IsValid => Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;
}



using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.ValueObjects;

public class GeoLocationTests
{
    [Fact]
    public void Constructor_WithValidCoordinates_ShouldCreateInstance()
    {
        // Arrange & Act
        var location = new GeoLocation(40.7128, -74.0060);

        // Assert
        Assert.Equal(40.7128, location.Latitude);
        Assert.Equal(-74.0060, location.Longitude);
        Assert.True(location.IsValid);
    }

    [Fact]
    public void Constructor_WithAllProperties_ShouldCreateInstance()
    {
        // Arrange & Act
        var location = new GeoLocation(
            latitude: 40.7128,
            longitude: -74.0060,
            address: "123 Main St",
            city: "New York",
            state: "NY",
            postalCode: "10001",
            country: "US",
            formattedAddress: "123 Main St, New York, NY 10001",
            placeId: "ChIJN1t_tDeuEmsRUsoyG83frY4");

        // Assert
        Assert.Equal("123 Main St", location.Address);
        Assert.Equal("New York", location.City);
        Assert.Equal("NY", location.State);
        Assert.Equal("10001", location.PostalCode);
        Assert.Equal("US", location.Country);
        Assert.Equal("123 Main St, New York, NY 10001", location.FormattedAddress);
        Assert.Equal("ChIJN1t_tDeuEmsRUsoyG83frY4", location.PlaceId);
    }

    [Fact]
    public void Constructor_WithInvalidLatitude_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(91, -74.0060));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(-91, -74.0060));
    }

    [Fact]
    public void Constructor_WithInvalidLongitude_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(40.7128, 181));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(40.7128, -181));
    }

    [Fact]
    public void Constructor_WithNullCountry_ShouldDefaultToUS()
    {
        // Arrange & Act
        var location = new GeoLocation(40.7128, -74.0060, country: null);

        // Assert
        Assert.Equal("US", location.Country);
    }
}





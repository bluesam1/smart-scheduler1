using SmartScheduler.Domain.Scheduling.Utilities;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class HaversineCalculatorTests
{
    [Fact]
    public void CalculateDistance_SamePoint_ReturnsZero()
    {
        // Arrange
        var lat = 40.7128;
        var lon = -74.0060;

        // Act
        var distance = HaversineCalculator.CalculateDistance(lat, lon, lat, lon);

        // Assert
        Assert.Equal(0, distance, 1); // Allow small floating point error
    }

    [Fact]
    public void CalculateDistance_NewYorkToLosAngeles_ReturnsApproximateDistance()
    {
        // Arrange
        // New York: 40.7128° N, 74.0060° W
        // Los Angeles: 34.0522° N, 118.2437° W
        // Approximate distance: ~3944 km = ~3,944,000 meters
        var nyLat = 40.7128;
        var nyLon = -74.0060;
        var laLat = 34.0522;
        var laLon = -118.2437;

        // Act
        var distance = HaversineCalculator.CalculateDistance(nyLat, nyLon, laLat, laLon);

        // Assert
        // Allow 5% tolerance for approximate calculation
        var expectedDistance = 3_944_000; // meters
        Assert.InRange(distance, expectedDistance * 0.95, expectedDistance * 1.05);
    }

    [Fact]
    public void CalculateDistance_ShortDistance_ReturnsAccurateResult()
    {
        // Arrange
        // Two points approximately 1 km apart in New York
        var lat1 = 40.7128;
        var lon1 = -74.0060;
        var lat2 = 40.7218; // ~1 km north
        var lon2 = -74.0060;

        // Act
        var distance = HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        // Should be approximately 1 km = 1000 meters
        // Allow 10% tolerance
        Assert.InRange(distance, 900, 1100);
    }

    [Fact]
    public void CalculateDistance_AntipodalPoints_ReturnsApproximateHalfCircumference()
    {
        // Arrange
        // Antipodal points (opposite sides of Earth)
        // North Pole area: 90° N, 0° E
        // South Pole area: 90° S, 0° E
        // Distance should be approximately half Earth's circumference
        var lat1 = 90.0;
        var lon1 = 0.0;
        var lat2 = -90.0;
        var lon2 = 0.0;

        // Act
        var distance = HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        // Half circumference ≈ π × Earth radius ≈ 3.14159 × 6,371,000 ≈ 20,000,000 meters
        var expectedDistance = 20_000_000; // meters
        Assert.InRange(distance, expectedDistance * 0.95, expectedDistance * 1.05);
    }

    [Fact]
    public void CalculateDistance_CrossEquator_ReturnsCorrectDistance()
    {
        // Arrange
        // Point in Northern Hemisphere: 10° N, 0° E
        // Point in Southern Hemisphere: 10° S, 0° E
        // Distance should be approximately 20° of latitude = ~2,222 km
        var lat1 = 10.0;
        var lon1 = 0.0;
        var lat2 = -10.0;
        var lon2 = 0.0;

        // Act
        var distance = HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        // 20° of latitude ≈ 2,222 km = 2,222,000 meters
        var expectedDistance = 2_222_000; // meters
        Assert.InRange(distance, expectedDistance * 0.95, expectedDistance * 1.05);
    }

    [Fact]
    public void CalculateDistance_CrossPrimeMeridian_ReturnsCorrectDistance()
    {
        // Arrange
        // Point west of Prime Meridian: 0° N, 10° W
        // Point east of Prime Meridian: 0° N, 10° E
        // Distance should be approximately 20° of longitude at equator
        var lat1 = 0.0;
        var lon1 = -10.0;
        var lat2 = 0.0;
        var lon2 = 10.0;

        // Act
        var distance = HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        // At equator, 20° of longitude ≈ 2,222 km = 2,222,000 meters
        var expectedDistance = 2_222_000; // meters
        Assert.InRange(distance, expectedDistance * 0.95, expectedDistance * 1.05);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0)] // Same point
    [InlineData(40.7128, -74.0060, 40.7128, -74.0060, 0.0)] // Same point (NYC)
    public void CalculateDistance_VariousSamePoints_ReturnsZero(
        double lat1, double lon1, double lat2, double lon2, double expectedDistance)
    {
        // Act
        var distance = HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        Assert.Equal(expectedDistance, distance, 1); // Allow small floating point error
    }

    [Fact]
    public void CalculateDistance_Performance_IsFast()
    {
        // Arrange
        var lat1 = 40.7128;
        var lon1 = -74.0060;
        var lat2 = 34.0522;
        var lon2 = -118.2437;

        // Act
        var start = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            HaversineCalculator.CalculateDistance(lat1, lon1, lat2, lon2);
        }
        var elapsed = DateTime.UtcNow - start;

        // Assert
        // Should complete 1000 calculations in well under 1 second
        Assert.True(elapsed.TotalMilliseconds < 1000, 
            $"1000 calculations took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }
}


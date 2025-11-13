namespace SmartScheduler.Domain.Scheduling.Utilities;

/// <summary>
/// Calculator for Haversine distance (great-circle distance between two points on Earth).
/// Used for fast, approximate distance calculations for initial filtering.
/// </summary>
public static class HaversineCalculator
{
    /// <summary>
    /// Earth's radius in meters (mean radius).
    /// </summary>
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculates the great-circle distance between two points on Earth using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees</param>
    /// <param name="lon1">Longitude of first point in degrees</param>
    /// <param name="lat2">Latitude of second point in degrees</param>
    /// <param name="lon2">Longitude of second point in degrees</param>
    /// <returns>Distance in meters</returns>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert degrees to radians
        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);
        var deltaLatRad = DegreesToRadians(lat2 - lat1);
        var deltaLonRad = DegreesToRadians(lon2 - lon1);

        // Haversine formula
        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}


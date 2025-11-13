using Microsoft.Extensions.Logging;
using SmartScheduler.Domain.Scheduling.Utilities;

namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for distance and ETA calculations with fallback to Haversine.
/// Provides graceful degradation when ORS is unavailable.
/// </summary>
public class DistanceCalculationService : IDistanceCalculationService
{
    private readonly SmartScheduler.Application.Contracts.Services.IDistanceService _distanceService;
    private readonly ILogger<DistanceCalculationService> _logger;
    private const double AverageSpeedKmh = 50.0; // Average driving speed for ETA estimation
    private const double MetersPerKm = 1000.0;

    public DistanceCalculationService(
        IDistanceService distanceService,
        ILogger<DistanceCalculationService> logger)
    {
        _distanceService = distanceService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DistanceResult> CalculateDistanceAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try ORS first
            var distance = await _distanceService.GetDistanceAsync(
                originLat, originLng, destinationLat, destinationLng, cancellationToken);

            if (distance.HasValue)
            {
                return new DistanceResult
                {
                    DistanceMeters = distance.Value,
                    IsDegraded = false,
                    Source = "ORS"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ORS distance calculation failed, falling back to Haversine");
        }

        // Fallback to Haversine
        try
        {
            var haversineDistance = HaversineCalculator.CalculateDistance(
                originLat, originLng, destinationLat, destinationLng);

            _logger.LogInformation("Using Haversine fallback for distance calculation");

            return new DistanceResult
            {
                DistanceMeters = haversineDistance,
                IsDegraded = true,
                Source = "Haversine"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Haversine distance calculation failed");
            return new DistanceResult
            {
                DistanceMeters = null,
                IsDegraded = true,
                Source = null
            };
        }
    }

    /// <inheritdoc />
    public async Task<EtaResult> CalculateEtaAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try ORS first
            var eta = await _distanceService.GetEtaAsync(
                originLat, originLng, destinationLat, destinationLng, cancellationToken);

            if (eta.HasValue)
            {
                return new EtaResult
                {
                    EtaMinutes = eta.Value,
                    IsDegraded = false,
                    Source = "ORS"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ORS ETA calculation failed, falling back to Haversine");
        }

        // Fallback to Haversine-based ETA estimation
        try
        {
            var haversineDistance = HaversineCalculator.CalculateDistance(
                originLat, originLng, destinationLat, destinationLng);

            // Estimate ETA: distance / average speed
            // Convert meters to km, then divide by speed (km/h), convert to minutes
            var distanceKm = haversineDistance / MetersPerKm;
            var etaHours = distanceKm / AverageSpeedKmh;
            var etaMinutes = (int)Math.Ceiling(etaHours * 60);

            _logger.LogInformation("Using Haversine fallback for ETA calculation (estimated {EtaMinutes} minutes)", etaMinutes);

            return new EtaResult
            {
                EtaMinutes = etaMinutes,
                IsDegraded = true,
                Source = "Haversine"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Haversine ETA calculation failed");
            return new EtaResult
            {
                EtaMinutes = null,
                IsDegraded = true,
                Source = null
            };
        }
    }
}


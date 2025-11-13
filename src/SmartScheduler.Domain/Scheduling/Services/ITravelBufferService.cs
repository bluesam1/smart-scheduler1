namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Interface for calculating travel buffers between job assignments.
/// </summary>
public interface ITravelBufferService
{
    /// <summary>
    /// Calculates travel buffer time based on ETA and regional configuration.
    /// Formula: max(10m, min(45m, ETA × 0.25))
    /// </summary>
    /// <param name="etaMinutes">Estimated travel time in minutes</param>
    /// <param name="regionalMultiplier">Optional regional multiplier (default 1.0)</param>
    /// <returns>Buffer time in minutes</returns>
    int CalculateBuffer(int etaMinutes, double regionalMultiplier = 1.0);

    /// <summary>
    /// Calculates buffer from contractor base to first job.
    /// </summary>
    /// <param name="etaMinutes">Estimated travel time from base to first job in minutes</param>
    /// <param name="regionalMultiplier">Optional regional multiplier (default 1.0)</param>
    /// <returns>Buffer time in minutes</returns>
    int CalculateBaseToFirstBuffer(int etaMinutes, double regionalMultiplier = 1.0);

    /// <summary>
    /// Calculates buffer between sequential jobs.
    /// </summary>
    /// <param name="etaMinutes">Estimated travel time between jobs in minutes</param>
    /// <param name="regionalMultiplier">Optional regional multiplier (default 1.0)</param>
    /// <returns>Buffer time in minutes</returns>
    int CalculateJobToJobBuffer(int etaMinutes, double regionalMultiplier = 1.0);

    /// <summary>
    /// Calculates optional buffer from last job back to base.
    /// </summary>
    /// <param name="etaMinutes">Estimated travel time from last job to base in minutes</param>
    /// <param name="regionalMultiplier">Optional regional multiplier (default 1.0)</param>
    /// <returns>Buffer time in minutes</returns>
    int CalculateLastToBaseBuffer(int etaMinutes, double regionalMultiplier = 1.0);
}


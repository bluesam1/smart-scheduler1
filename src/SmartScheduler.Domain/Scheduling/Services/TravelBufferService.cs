namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Service for calculating travel buffers between job assignments.
/// Implements the travel buffer policy: max(10m, min(45m, ETA × 0.25))
/// </summary>
public class TravelBufferService : ITravelBufferService
{
    private const int MinimumBufferMinutes = 10;
    private const int MaximumBufferMinutes = 45;
    private const double EtaMultiplier = 0.25;

    /// <inheritdoc />
    public int CalculateBuffer(int etaMinutes, double regionalMultiplier = 1.0)
    {
        if (etaMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(etaMinutes), "ETA cannot be negative.");

        if (regionalMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(regionalMultiplier), "Regional multiplier must be positive.");

        // Handle edge case: same location (ETA = 0 or very small)
        if (etaMinutes == 0)
        {
            return MinimumBufferMinutes;
        }

        // Calculate base buffer: ETA × 0.25
        var baseBuffer = etaMinutes * EtaMultiplier;

        // Apply regional multiplier
        var adjustedBuffer = baseBuffer * regionalMultiplier;

        // Apply min/max constraints: max(10m, min(45m, adjustedBuffer))
        var buffer = Math.Max(MinimumBufferMinutes, Math.Min(MaximumBufferMinutes, (int)Math.Round(adjustedBuffer)));

        return buffer;
    }

    /// <inheritdoc />
    public int CalculateBaseToFirstBuffer(int etaMinutes, double regionalMultiplier = 1.0)
    {
        return CalculateBuffer(etaMinutes, regionalMultiplier);
    }

    /// <inheritdoc />
    public int CalculateJobToJobBuffer(int etaMinutes, double regionalMultiplier = 1.0)
    {
        return CalculateBuffer(etaMinutes, regionalMultiplier);
    }

    /// <inheritdoc />
    public int CalculateLastToBaseBuffer(int etaMinutes, double regionalMultiplier = 1.0)
    {
        // Last-to-base buffer is optional and uses the same formula
        // In the future, this could be made configurable (e.g., optional, different formula)
        return CalculateBuffer(etaMinutes, regionalMultiplier);
    }
}


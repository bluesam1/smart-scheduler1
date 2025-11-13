using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Scheduling.Services;

/// <summary>
/// Generates feasible time slots for contractors with different optimization goals.
/// </summary>
public class SlotGenerator : ISlotGenerator
{
    private readonly IAvailabilityEngine _availabilityEngine;
    private readonly ITravelBufferService _travelBufferService;
    private readonly IFatigueCalculator _fatigueCalculator;

    public SlotGenerator(
        IAvailabilityEngine availabilityEngine,
        ITravelBufferService travelBufferService,
        IFatigueCalculator fatigueCalculator)
    {
        _availabilityEngine = availabilityEngine ?? throw new ArgumentNullException(nameof(availabilityEngine));
        _travelBufferService = travelBufferService ?? throw new ArgumentNullException(nameof(travelBufferService));
        _fatigueCalculator = fatigueCalculator ?? throw new ArgumentNullException(nameof(fatigueCalculator));
    }

    /// <inheritdoc />
    public IReadOnlyList<GeneratedSlot> GenerateSlots(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar = null,
        int? baseToJobEtaMinutes = null,
        int? previousJobToJobEtaMinutes = null,
        int contractorRating = 50,
        bool isRushJob = false)
    {
        if (jobDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(jobDurationMinutes), "Job duration must be positive.");

        // Estimate buffer to request larger windows from availability engine
        // Use a reasonable estimate (15 minutes) or actual if available
        var estimatedBuffer = baseToJobEtaMinutes.HasValue
            ? _travelBufferService.CalculateBaseToFirstBuffer(baseToJobEtaMinutes.Value)
            : previousJobToJobEtaMinutes.HasValue
                ? _travelBufferService.CalculateJobToJobBuffer(previousJobToJobEtaMinutes.Value)
                : 15; // Default estimate

        // Request slots that can fit buffer + job duration
        var totalTimeNeeded = estimatedBuffer + jobDurationMinutes;

        // Get available slots from availability engine
        var availableSlots = _availabilityEngine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            totalTimeNeeded,
            contractorTimezone,
            jobTimezone,
            calendar);

        if (availableSlots.Count == 0)
        {
            return Array.Empty<GeneratedSlot>();
        }

        var generatedSlots = new List<GeneratedSlot>();

        // Generate earliest slot
        var earliestSlot = GenerateEarliestSlot(availableSlots, jobDurationMinutes, baseToJobEtaMinutes, existingAssignments, contractorTimezone, isRushJob);
        if (earliestSlot != null)
        {
            generatedSlots.Add(earliestSlot);
        }

        // Generate lowest-travel slot (if we have travel time info)
        if (previousJobToJobEtaMinutes.HasValue || baseToJobEtaMinutes.HasValue)
        {
            var lowestTravelSlot = GenerateLowestTravelSlot(
                availableSlots,
                jobDurationMinutes,
                previousJobToJobEtaMinutes,
                baseToJobEtaMinutes,
                existingAssignments,
                contractorTimezone,
                isRushJob);
            if (lowestTravelSlot != null)
            {
                generatedSlots.Add(lowestTravelSlot);
            }
        }

        // Generate highest-confidence slot
        var highestConfidenceSlot = GenerateHighestConfidenceSlot(
            availableSlots,
            jobDurationMinutes,
            baseToJobEtaMinutes,
            contractorRating,
            existingAssignments,
            contractorTimezone,
            isRushJob);
        if (highestConfidenceSlot != null)
        {
            generatedSlots.Add(highestConfidenceSlot);
        }

        // Remove duplicates (same window and type) and return up to 3
        // Note: Fatigue checks are already performed in individual slot generation methods
        return generatedSlots
            .GroupBy(s => new { s.Window.Start, s.Type })
            .Select(g => g.First())
            .Take(3)
            .ToList();
    }

    /// <summary>
    /// Generates the earliest feasible slot.
    /// </summary>
    private GeneratedSlot? GenerateEarliestSlot(
        IReadOnlyList<TimeWindow> availableSlots,
        int jobDurationMinutes,
        int? baseToJobEtaMinutes,
        IReadOnlyList<TimeWindow> existingAssignments,
        string contractorTimezone,
        bool isRushJob)
    {
        if (availableSlots.Count == 0)
            return null;

        // Calculate buffer if we have ETA
        var bufferMinutes = 0;
        if (baseToJobEtaMinutes.HasValue)
        {
            bufferMinutes = _travelBufferService.CalculateBaseToFirstBuffer(baseToJobEtaMinutes.Value);
        }

        // Total time needed: buffer + job duration
        var totalTimeNeeded = bufferMinutes + jobDurationMinutes;

        // Find earliest window that can fit buffer + job duration
        var earliestWindow = availableSlots
            .Where(w => (int)(w.End - w.Start).TotalMinutes >= totalTimeNeeded)
            .OrderBy(s => s.Start)
            .FirstOrDefault();

        if (earliestWindow == null)
            return null;

        // Calculate slot start (earliest possible, accounting for buffer)
        var slotStart = earliestWindow.Start.AddMinutes(bufferMinutes);
        var slotEnd = slotStart.AddMinutes(jobDurationMinutes);

        // Verify slot fits in window (should always be true due to check above)
        if (slotEnd > earliestWindow.End)
        {
            return null;
        }

        // Check fatigue limits
        var proposedSlot = new TimeWindow(slotStart, slotEnd);
        var fatigueCheck = _fatigueCalculator.CheckFeasibility(
            proposedSlot,
            existingAssignments ?? Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            contractorTimezone,
            isRushJob);

        if (!fatigueCheck.IsFeasible)
        {
            return null; // Slot violates fatigue limits
        }

        // Calculate confidence for earliest slot
        var confidence = CalculateConfidence(earliestWindow, baseToJobEtaMinutes, null, 50);

        return new GeneratedSlot
        {
            Window = proposedSlot,
            Type = SlotType.Earliest,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Generates the slot with lowest travel time.
    /// </summary>
    private GeneratedSlot? GenerateLowestTravelSlot(
        IReadOnlyList<TimeWindow> availableSlots,
        int jobDurationMinutes,
        int? previousJobToJobEtaMinutes,
        int? baseToJobEtaMinutes,
        IReadOnlyList<TimeWindow> existingAssignments,
        string contractorTimezone,
        bool isRushJob)
    {
        if (availableSlots.Count == 0)
            return null;

        // Use previous job ETA if available, otherwise base-to-job ETA
        var etaMinutes = previousJobToJobEtaMinutes ?? baseToJobEtaMinutes;
        if (!etaMinutes.HasValue)
            return null;

        // Calculate buffer
        var bufferMinutes = previousJobToJobEtaMinutes.HasValue
            ? _travelBufferService.CalculateJobToJobBuffer(previousJobToJobEtaMinutes.Value)
            : _travelBufferService.CalculateBaseToFirstBuffer(baseToJobEtaMinutes!.Value);

        var totalTimeNeeded = bufferMinutes + jobDurationMinutes;

        // Find slot that minimizes total time (buffer + job duration)
        // For now, we'll use the earliest slot that fits after buffer
        // In a more sophisticated implementation, we'd consider all slots and pick the one with minimum travel
        var candidateSlots = availableSlots
            .Where(w => (int)(w.End - w.Start).TotalMinutes >= totalTimeNeeded)
            .Select(window =>
            {
                var slotStart = window.Start.AddMinutes(bufferMinutes);
                var slotEnd = slotStart.AddMinutes(jobDurationMinutes);
                if (slotEnd <= window.End)
                {
                    return new { Window = new TimeWindow(slotStart, slotEnd), TravelTime = etaMinutes.Value, OriginalWindow = window };
                }
                return null;
            })
            .Where(s => s != null)
            .OrderBy(s => s!.TravelTime)
            .FirstOrDefault();

        if (candidateSlots == null)
            return null;

        // Check fatigue limits
        var fatigueCheck = _fatigueCalculator.CheckFeasibility(
            candidateSlots.Window,
            existingAssignments ?? Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            contractorTimezone,
            isRushJob);

        if (!fatigueCheck.IsFeasible)
        {
            return null; // Slot violates fatigue limits
        }

        var confidence = CalculateConfidence(
            candidateSlots.OriginalWindow,
            baseToJobEtaMinutes,
            previousJobToJobEtaMinutes,
            50);

        return new GeneratedSlot
        {
            Window = candidateSlots.Window,
            Type = SlotType.LowestTravel,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Generates the slot with highest confidence.
    /// </summary>
    private GeneratedSlot? GenerateHighestConfidenceSlot(
        IReadOnlyList<TimeWindow> availableSlots,
        int jobDurationMinutes,
        int? baseToJobEtaMinutes,
        int contractorRating,
        IReadOnlyList<TimeWindow> existingAssignments,
        string contractorTimezone,
        bool isRushJob)
    {
        if (availableSlots.Count == 0)
            return null;

        // Calculate buffer if we have ETA
        var bufferMinutes = baseToJobEtaMinutes.HasValue
            ? _travelBufferService.CalculateBaseToFirstBuffer(baseToJobEtaMinutes.Value)
            : 0;

        var totalTimeNeeded = bufferMinutes + jobDurationMinutes;

        // Calculate confidence for each available slot that can fit buffer + job duration
        var slotsWithConfidence = availableSlots
            .Where(w => (int)(w.End - w.Start).TotalMinutes >= totalTimeNeeded)
            .Select(window =>
            {
                var slotStart = window.Start.AddMinutes(bufferMinutes);
                var slotEnd = slotStart.AddMinutes(jobDurationMinutes);

                if (slotEnd > window.End)
                    return null;

                var confidence = CalculateConfidence(window, baseToJobEtaMinutes, null, contractorRating);

                return new
                {
                    Window = new TimeWindow(slotStart, slotEnd),
                    Confidence = confidence
                };
            })
            .Where(s => s != null)
            .OrderByDescending(s => s!.Confidence)
            .FirstOrDefault();

        if (slotsWithConfidence == null)
            return null;

        // Check fatigue limits
        var fatigueCheck = _fatigueCalculator.CheckFeasibility(
            slotsWithConfidence.Window,
            existingAssignments ?? Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            contractorTimezone,
            isRushJob);

        if (!fatigueCheck.IsFeasible)
        {
            return null; // Slot violates fatigue limits
        }

        return new GeneratedSlot
        {
            Window = slotsWithConfidence.Window,
            Type = SlotType.HighestConfidence,
            Confidence = slotsWithConfidence.Confidence
        };
    }

    /// <summary>
    /// Calculates confidence score (0-100) for a slot based on various factors.
    /// </summary>
    private int CalculateConfidence(
        TimeWindow availableWindow,
        int? baseToJobEtaMinutes,
        int? previousJobToJobEtaMinutes,
        int contractorRating)
    {
        var confidence = 50; // Base confidence

        // Factor 1: Availability window size (larger window = higher confidence)
        var windowSizeMinutes = (int)(availableWindow.End - availableWindow.Start).TotalMinutes;
        var windowSizeScore = Math.Min(100, windowSizeMinutes / 10); // 10 minutes = 1 point, max 100
        confidence += (int)(windowSizeScore * 0.2); // 20% weight

        // Factor 2: Travel time (shorter travel = higher confidence)
        var travelTime = previousJobToJobEtaMinutes ?? baseToJobEtaMinutes ?? 0;
        var travelScore = travelTime == 0 ? 100 : Math.Max(0, 100 - (travelTime / 2)); // Inverse relationship
        confidence += (int)(travelScore * 0.2); // 20% weight

        // Factor 3: Contractor rating (higher rating = higher confidence)
        confidence += (int)(contractorRating * 0.6); // 60% weight

        // Normalize to 0-100
        return Math.Max(0, Math.Min(100, confidence));
    }
}


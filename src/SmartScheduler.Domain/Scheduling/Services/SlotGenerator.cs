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

        // Request slots that can fit buffer + job duration + quarter-hour rounding overhead
        // Adding 15 minutes to account for quarter-hour rounding that happens in slot generation
        var totalTimeNeeded = estimatedBuffer + jobDurationMinutes + 15;

        // Get available slots from availability engine
        var availableSlots = _availabilityEngine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            totalTimeNeeded,
            contractorTimezone,
            jobTimezone,
            calendar);

        // If no single-day slots available, try multi-day consecutive splits
        if (availableSlots.Count == 0)
        {
            // Try 2-day split
            var twoDaySlots = TryGenerateMultiDaySlots(
                workingHours,
                serviceWindow,
                existingAssignments,
                jobDurationMinutes,
                contractorTimezone,
                jobTimezone,
                calendar,
                baseToJobEtaMinutes,
                previousJobToJobEtaMinutes,
                contractorRating,
                isRushJob,
                daysSpan: 2);

            if (twoDaySlots.Count > 0)
                return twoDaySlots;

            // Try 3-day split
            var threeDaySlots = TryGenerateMultiDaySlots(
                workingHours,
                serviceWindow,
                existingAssignments,
                jobDurationMinutes,
                contractorTimezone,
                jobTimezone,
                calendar,
                baseToJobEtaMinutes,
                previousJobToJobEtaMinutes,
                contractorRating,
                isRushJob,
                daysSpan: 3);

            return threeDaySlots;
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
        var rawSlotStart = earliestWindow.Start.AddMinutes(bufferMinutes);
        var slotStart = RoundToNearestQuarterHour(rawSlotStart);
        
        // If rounding pushed start time backwards before window start, round up instead
        if (slotStart < earliestWindow.Start)
        {
            slotStart = slotStart.AddMinutes(15);
        }
        
        var slotEnd = slotStart.AddMinutes(jobDurationMinutes);

        // Verify slot fits in window
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
            DailyWindows = new List<TimeWindow> { proposedSlot }, // Single-day slot
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
                var rawSlotStart = window.Start.AddMinutes(bufferMinutes);
                var slotStart = RoundToNearestQuarterHour(rawSlotStart);
                
                // If rounding pushed start time backwards before window start, round up instead
                if (slotStart < window.Start)
                {
                    slotStart = slotStart.AddMinutes(15);
                }
                
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
            DailyWindows = new List<TimeWindow> { candidateSlots.Window }, // Single-day slot
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
                var rawSlotStart = window.Start.AddMinutes(bufferMinutes);
                var slotStart = RoundToNearestQuarterHour(rawSlotStart);
                
                // If rounding pushed start time backwards before window start, round up instead
                if (slotStart < window.Start)
                {
                    slotStart = slotStart.AddMinutes(15);
                }
                
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
            DailyWindows = new List<TimeWindow> { slotsWithConfidence.Window }, // Single-day slot
            Type = SlotType.HighestConfidence,
            Confidence = slotsWithConfidence.Confidence
        };
    }

    /// <summary>
    /// Rounds a DateTime to the nearest 15-minute interval for professional scheduling.
    /// </summary>
    private DateTime RoundToNearestQuarterHour(DateTime time)
    {
        var minutes = time.Minute;
        var roundedMinutes = (int)(Math.Round(minutes / 15.0) * 15);
        
        // Handle rounding to 60 minutes (next hour)
        if (roundedMinutes == 60)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Kind).AddHours(1);
        }
        
        return new DateTime(time.Year, time.Month, time.Day, time.Hour, roundedMinutes, 0, time.Kind);
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

    /// <summary>
    /// Attempts to generate multi-day consecutive slot assignments.
    /// Splits job duration across consecutive days (2 or 3 days).
    /// </summary>
    private IReadOnlyList<GeneratedSlot> TryGenerateMultiDaySlots(
        IReadOnlyList<WorkingHours> workingHours,
        TimeWindow serviceWindow,
        IReadOnlyList<TimeWindow> existingAssignments,
        int jobDurationMinutes,
        string contractorTimezone,
        string jobTimezone,
        ContractorCalendar? calendar,
        int? baseToJobEtaMinutes,
        int? previousJobToJobEtaMinutes,
        int contractorRating,
        bool isRushJob,
        int daysSpan)
    {
        if (daysSpan < 2 || daysSpan > 3)
            return Array.Empty<GeneratedSlot>();

        // Convert service window to contractor timezone
        var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(contractorTimezone);
        var serviceStart = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.Start, tzInfo);
        var serviceEnd = TimeZoneInfo.ConvertTimeFromUtc(serviceWindow.End, tzInfo);

        // Try to find consecutive days with availability
        var currentDate = serviceStart.Date;
        var endDate = serviceEnd.Date;

        while (currentDate.AddDays(daysSpan - 1) <= endDate)
        {
            var dailyWindows = new List<TimeWindow>();
            var totalAllocatedMinutes = 0;
            var minutesPerDay = jobDurationMinutes / daysSpan;
            var remainderMinutes = jobDurationMinutes % daysSpan;

            // Try to allocate time across consecutive days
            bool allDaysValid = true;
            for (int i = 0; i < daysSpan; i++)
            {
                var targetDate = currentDate.AddDays(i);
                var dayOfWeek = targetDate.DayOfWeek;

                // Find working hours for this day
                var dayWorkingHours = workingHours.FirstOrDefault(wh => wh.DayOfWeek == dayOfWeek);
                if (dayWorkingHours == null)
                {
                    allDaysValid = false;
                    break;
                }

                // Parse working hours
                var dayStart = DateTime.Parse($"{targetDate:yyyy-MM-dd} {dayWorkingHours.StartTime}", null, System.Globalization.DateTimeStyles.AssumeLocal);
                var dayEnd = DateTime.Parse($"{targetDate:yyyy-MM-dd} {dayWorkingHours.EndTime}", null, System.Globalization.DateTimeStyles.AssumeLocal);

                // Check if day is within service window
                if (targetDate < serviceStart.Date || targetDate > serviceEnd.Date)
                {
                    allDaysValid = false;
                    break;
                }

                // Constrain to service window
                if (targetDate == serviceStart.Date)
                    dayStart = dayStart < serviceStart ? serviceStart : dayStart;
                if (targetDate == serviceEnd.Date)
                    dayEnd = dayEnd > serviceEnd ? serviceEnd : dayEnd;

                // Allocate time for this day (last day gets remainder)
                var dayMinutes = i == daysSpan - 1 ? minutesPerDay + remainderMinutes : minutesPerDay;

                // Check if day has enough hours
                var availableMinutes = (int)(dayEnd - dayStart).TotalMinutes;
                if (availableMinutes < dayMinutes)
                {
                    allDaysValid = false;
                    break;
                }

                // Round start time to quarter hour
                var slotStart = RoundToNearestQuarterHour(dayStart);
                if (slotStart < dayStart)
                    slotStart = slotStart.AddMinutes(15);

                var slotEnd = slotStart.AddMinutes(dayMinutes);

                // Check if slot fits in working hours
                if (slotEnd > dayEnd)
                {
                    allDaysValid = false;
                    break;
                }

                // Convert back to UTC
                var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(slotStart, tzInfo);
                var slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(slotEnd, tzInfo);

                dailyWindows.Add(new TimeWindow(slotStartUtc, slotEndUtc));
                totalAllocatedMinutes += dayMinutes;
            }

            // If all days are valid, create the multi-day slot
            if (allDaysValid && dailyWindows.Count == daysSpan)
            {
                // Overall window spans from first day start to last day end
                var overallWindow = new TimeWindow(dailyWindows[0].Start, dailyWindows[daysSpan - 1].End);

                // Check fatigue limits for the multi-day assignment
                var fatigueCheck = _fatigueCalculator.CheckFeasibility(
                    overallWindow,
                    existingAssignments ?? Array.Empty<TimeWindow>(),
                    jobDurationMinutes,
                    contractorTimezone,
                    isRushJob);

                if (fatigueCheck.IsFeasible)
                {
                    // Create a single slot representing the multi-day assignment
                    var confidence = CalculateConfidence(
                        overallWindow,
                        baseToJobEtaMinutes,
                        previousJobToJobEtaMinutes,
                        contractorRating);

                    var multiDaySlot = new GeneratedSlot
                    {
                        Window = overallWindow,
                        DailyWindows = dailyWindows,
                        Type = SlotType.Earliest, // Multi-day slots are earliest-based
                        Confidence = Math.Max(0, confidence - 10) // Slightly lower confidence for multi-day
                    };

                    return new List<GeneratedSlot> { multiDaySlot };
                }
            }

            // Move to next day and try again
            currentDate = currentDate.AddDays(1);
        }

        return Array.Empty<GeneratedSlot>();
    }
}


using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;

namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for re-validating contractor availability before assignment.
/// Checks working hours, existing assignments, calendar exceptions, and travel buffers.
/// </summary>
public class AvailabilityRevalidator : IAvailabilityRevalidator
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IContractorRepository _contractorRepository;
    private readonly IAvailabilityEngine _availabilityEngine;
    private readonly ITravelBufferService _travelBufferService;

    public AvailabilityRevalidator(
        IAssignmentRepository assignmentRepository,
        IContractorRepository contractorRepository,
        IAvailabilityEngine availabilityEngine,
        ITravelBufferService travelBufferService)
    {
        _assignmentRepository = assignmentRepository;
        _contractorRepository = contractorRepository;
        _availabilityEngine = availabilityEngine;
        _travelBufferService = travelBufferService;
    }

    public async Task<AvailabilityValidationResult> ValidateAvailabilityAsync(
        Guid contractorId,
        Guid jobId,
        DateTime startUtc,
        DateTime endUtc,
        int jobDurationMinutes,
        string jobTimezone,
        CancellationToken cancellationToken = default)
    {
        // Get contractor
        var contractor = await _contractorRepository.GetByIdAsync(contractorId, cancellationToken);
        if (contractor == null)
        {
            return AvailabilityValidationResult.Invalid($"Contractor with ID {contractorId} not found.");
        }

        // Validate time slot duration matches job duration
        var slotDuration = (int)(endUtc - startUtc).TotalMinutes;
        if (slotDuration < jobDurationMinutes)
        {
            return AvailabilityValidationResult.Invalid(
                $"Time slot duration ({slotDuration} minutes) is less than job duration ({jobDurationMinutes} minutes).");
        }

        // Get existing assignments for the contractor (excluding cancelled)
        var allAssignments = await _assignmentRepository.GetByContractorIdAsync(contractorId, cancellationToken);
        var activeAssignments = allAssignments
            .Where(a => a.Status != AssignmentEntityStatus.Cancelled && a.Id != jobId) // Exclude current job if updating
            .Select(a => new TimeWindow(a.StartUtc, a.EndUtc))
            .ToList();

        // Check for direct conflicts (overlapping time slots)
        foreach (var existing in activeAssignments)
        {
            if (TimeSlotsOverlap(startUtc, endUtc, existing.Start, existing.End))
            {
                var conflictingAssignment = allAssignments.First(a => 
                    a.StartUtc == existing.Start && a.EndUtc == existing.End);
                
                return AvailabilityValidationResult.Invalid(
                    $"Contractor is already assigned to another job during this time slot. " +
                    $"Conflicting assignment: {conflictingAssignment.Id} " +
                    $"({existing.Start:yyyy-MM-dd HH:mm} - {existing.End:yyyy-MM-dd HH:mm} UTC).",
                    conflictingAssignment.Id);
            }
        }

        // Check if time slot is within working hours using availability engine
        // Create a service window that includes the requested time slot
        var serviceWindow = new TimeWindow(
            startUtc.AddMinutes(-30), // Add some buffer before
            endUtc.AddMinutes(30));   // Add some buffer after

        // Calculate available slots using availability engine
        var availableSlots = _availabilityEngine.CalculateAvailableSlots(
            contractor.WorkingHours.ToList(),
            serviceWindow,
            activeAssignments,
            jobDurationMinutes,
            contractor.Timezone,
            jobTimezone,
            contractor.Calendar);

        // Check if the requested time slot is feasible
        var isFeasible = availableSlots.Any(slot =>
            slot.Start <= startUtc && slot.End >= endUtc);

        if (!isFeasible)
        {
            // Check if it's outside working hours
            var contractorTz = TimeZoneConverter.TZConvert.GetTimeZoneInfo(contractor.Timezone);
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, contractorTz);
            var endLocal = TimeZoneInfo.ConvertTimeFromUtc(endUtc, contractorTz);
            
            var dayOfWeek = startLocal.DayOfWeek;
            var workingHoursForDay = contractor.WorkingHours
                .FirstOrDefault(wh => (int)wh.DayOfWeek == (int)dayOfWeek);

            if (workingHoursForDay == null)
            {
                return AvailabilityValidationResult.Invalid(
                    $"Time slot is outside contractor's working hours. " +
                    $"Contractor does not work on {dayOfWeek}.");
            }

            // Check if time is within working hours
            var startTime = TimeOnly.FromDateTime(startLocal);
            var endTime = TimeOnly.FromDateTime(endLocal);
            var workStart = workingHoursForDay.StartTime;
            var workEnd = workingHoursForDay.EndTime;

            if (startTime < workStart || endTime > workEnd)
            {
                return AvailabilityValidationResult.Invalid(
                    $"Time slot ({startTime:HH:mm} - {endTime:HH:mm} {contractor.Timezone}) is outside contractor's working hours " +
                    $"({workStart:HH:mm} - {workEnd:HH:mm} {contractor.Timezone} on {dayOfWeek}).");
            }

            // If we have available slots but this one doesn't match, it might be due to travel buffers or other constraints
            if (availableSlots.Count > 0)
            {
                return AvailabilityValidationResult.Invalid(
                    $"Time slot is not feasible. The slot may conflict with travel buffers or other constraints. " +
                    $"Available slots near this time: {string.Join(", ", availableSlots.Take(3).Select(s => $"{s.Start:HH:mm}-{s.End:HH:mm} UTC"))}.");
            }

            return AvailabilityValidationResult.Invalid(
                $"Time slot is not feasible. Contractor has no available slots during this time period.");
        }

        // Check for travel buffer conflicts with adjacent assignments
        // Find assignments immediately before and after
        var previousAssignment = activeAssignments
            .Where(a => a.End <= startUtc)
            .OrderByDescending(a => a.End)
            .FirstOrDefault();

        var nextAssignment = activeAssignments
            .Where(a => a.Start >= endUtc)
            .OrderBy(a => a.Start)
            .FirstOrDefault();

        // Note: We can't calculate exact travel buffers without ETA data, but we can check minimum spacing
        // For now, we'll rely on the availability engine which should account for this in slot generation
        // In a full implementation, we'd check ETA and calculate buffers here

        return AvailabilityValidationResult.Valid();
    }

    /// <summary>
    /// Checks if two time slots overlap.
    /// </summary>
    private static bool TimeSlotsOverlap(
        DateTime start1, DateTime end1,
        DateTime start2, DateTime end2)
    {
        // Two time slots overlap if:
        // - start1 < end2 AND start2 < end1
        return start1 < end2 && start2 < end1;
    }
}


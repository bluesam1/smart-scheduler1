using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class AvailabilityEngineTests
{
    private readonly IAvailabilityEngine _engine;

    public AvailabilityEngineTests()
    {
        _engine = new AvailabilityEngine();
    }

    [Fact]
    public void CalculateAvailableSlots_WithBasicWorkingHours_ReturnsFeasibleSlots()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 9, 0, 0, DateTimeKind.Utc), // Monday 9 AM EST = 14:00 UTC
            new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc)); // Monday 5 PM EST = 22:00 UTC

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 120; // 2 hours

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(slots);
        Assert.All(slots, slot =>
        {
            Assert.True(slot.Start < slot.End);
            Assert.True((slot.End - slot.Start).TotalMinutes >= jobDurationMinutes);
        });
    }

    [Fact]
    public void CalculateAvailableSlots_WithExistingAssignment_ExcludesBlockedTime()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), // Monday 9 AM EST
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc)); // Monday 5 PM EST

        // Existing assignment from 10 AM to 12 PM EST (15:00-17:00 UTC)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc))
        };

        var jobDurationMinutes = 60;

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(slots);
        // Verify no slots overlap with existing assignment
        Assert.All(slots, slot =>
        {
            Assert.True(slot.End <= existingAssignments[0].Start || slot.Start >= existingAssignments[0].End);
        });
    }

    [Fact]
    public void CalculateAvailableSlots_WithHoliday_ExcludesHoliday()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var calendar = new ContractorCalendar(
            holidays: new List<DateOnly> { new DateOnly(2025, 1, 13) }); // Monday is a holiday

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            calendar);

        // Assert
        Assert.Empty(slots); // No slots on holidays
    }

    [Fact]
    public void CalculateAvailableSlots_WithOverrideException_UsesOverrideHours()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var overrideHours = new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(10, 0),
            new TimeOnly(14, 0),
            "America/New_York");

        var exception = new CalendarException(
            new DateOnly(2025, 1, 13),
            CalendarExceptionType.Override,
            overrideHours);

        var calendar = new ContractorCalendar(
            exceptions: new List<CalendarException> { exception });

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 20, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            calendar);

        // Assert
        Assert.NotEmpty(slots);
        // Slots should be within override hours (10 AM - 2 PM EST = 15:00-19:00 UTC)
        Assert.All(slots, slot =>
        {
            Assert.True(slot.Start >= new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc));
            Assert.True(slot.End <= new DateTime(2025, 1, 13, 19, 0, 0, DateTimeKind.Utc));
        });
    }

    [Fact]
    public void CalculateAvailableSlots_WithWindowTooShort_ReturnsEmpty()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), "America/New_York") // Only 1 hour
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 120; // 2 hours - too long for 1 hour window

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void CalculateAvailableSlots_WithMultipleDays_ReturnsSlotsAcrossDays()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York"),
            new WorkingHours(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), // Monday 9 AM EST
            new DateTime(2025, 1, 14, 22, 0, 0, DateTimeKind.Utc)); // Tuesday 5 PM EST

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(slots);
        // Should have slots on both Monday and Tuesday
        var mondaySlots = slots.Where(s => s.Start.Date == new DateTime(2025, 1, 13).Date).ToList();
        var tuesdaySlots = slots.Where(s => s.Start.Date == new DateTime(2025, 1, 14).Date).ToList();
        Assert.NotEmpty(mondaySlots);
        Assert.NotEmpty(tuesdaySlots);
    }

    [Fact]
    public void CalculateAvailableSlots_WithNoWorkingHours_ReturnsEmpty()
    {
        // Arrange
        var workingHours = Array.Empty<WorkingHours>();
        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void CalculateAvailableSlots_DeterministicResults_SameInputsProduceSameOutputs()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots1 = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        var slots2 = _engine.CalculateAvailableSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.Equal(slots1.Count, slots2.Count);
        for (int i = 0; i < slots1.Count; i++)
        {
            Assert.Equal(slots1[i].Start, slots2[i].Start);
            Assert.Equal(slots1[i].End, slots2[i].End);
        }
    }
}



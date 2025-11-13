using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class SlotGeneratorTests
{
    private readonly IAvailabilityEngine _availabilityEngine;
    private readonly ITravelBufferService _travelBufferService;
    private readonly IFatigueCalculator _fatigueCalculator;
    private readonly ISlotGenerator _slotGenerator;

    public SlotGeneratorTests()
    {
        _availabilityEngine = new AvailabilityEngine();
        _travelBufferService = new TravelBufferService();
        _fatigueCalculator = new FatigueCalculator();
        _slotGenerator = new SlotGenerator(_availabilityEngine, _travelBufferService, _fatigueCalculator);
    }

    [Fact]
    public void GenerateSlots_WithAvailableTime_ReturnsUpToThreeSlots()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), // Monday 9 AM EST
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc)); // Monday 5 PM EST

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 120;
        var baseToJobEtaMinutes = 30;

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: baseToJobEtaMinutes);

        // Assert
        Assert.NotEmpty(slots);
        Assert.True(slots.Count <= 3);
        Assert.All(slots, slot =>
        {
            Assert.NotNull(slot.Window);
            Assert.True(slot.Window.Start < slot.Window.End);
            Assert.True(slot.Confidence >= 0 && slot.Confidence <= 100);
        });
    }

    [Fact]
    public void GenerateSlots_IncludesEarliestSlot()
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
        var baseToJobEtaMinutes = 20;

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: baseToJobEtaMinutes);

        // Assert
        var earliestSlot = slots.FirstOrDefault(s => s.Type == SlotType.Earliest);
        Assert.NotNull(earliestSlot);
        Assert.True(earliestSlot.Window.Start >= serviceWindow.Start);
    }

    [Fact]
    public void GenerateSlots_WithPreviousJobEta_IncludesLowestTravelSlot()
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
        var previousJobToJobEtaMinutes = 15;

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            previousJobToJobEtaMinutes: previousJobToJobEtaMinutes);

        // Assert
        var lowestTravelSlot = slots.FirstOrDefault(s => s.Type == SlotType.LowestTravel);
        Assert.NotNull(lowestTravelSlot);
    }

    [Fact]
    public void GenerateSlots_IncludesHighestConfidenceSlot()
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
        var contractorRating = 80;

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            contractorRating: contractorRating);

        // Assert
        var highestConfidenceSlot = slots.FirstOrDefault(s => s.Type == SlotType.HighestConfidence);
        Assert.NotNull(highestConfidenceSlot);
        Assert.True(highestConfidenceSlot.Confidence >= 0 && highestConfidenceSlot.Confidence <= 100);
    }

    [Fact]
    public void GenerateSlots_WithNoAvailableTime_ReturnsEmpty()
    {
        // Arrange
        var workingHours = Array.Empty<WorkingHours>();
        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        var existingAssignments = Array.Empty<TimeWindow>();
        var jobDurationMinutes = 60;

        // Act
        var slots = _slotGenerator.GenerateSlots(
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
    public void GenerateSlots_WithExistingAssignment_ExcludesBlockedTime()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        // Existing assignment blocks 10 AM - 12 PM EST (15:00-17:00 UTC)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc))
        };

        var jobDurationMinutes = 60;

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.All(slots, slot =>
        {
            // Verify no slots overlap with existing assignment
            Assert.True(
                slot.Window.End <= existingAssignments[0].Start ||
                slot.Window.Start >= existingAssignments[0].End);
        });
    }

    [Fact]
    public void GenerateSlots_WithHighContractorRating_HigherConfidence()
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
        var slotsLowRating = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            contractorRating: 30);

        var slotsHighRating = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            contractorRating: 90);

        // Assert
        var lowConfidence = slotsLowRating.FirstOrDefault(s => s.Type == SlotType.HighestConfidence)?.Confidence ?? 0;
        var highConfidence = slotsHighRating.FirstOrDefault(s => s.Type == SlotType.HighestConfidence)?.Confidence ?? 0;
        Assert.True(highConfidence >= lowConfidence);
    }

    [Fact]
    public void GenerateSlots_DeterministicResults_SameInputsProduceSameOutputs()
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
        var baseToJobEtaMinutes = 30;

        // Act
        var slots1 = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: baseToJobEtaMinutes);

        var slots2 = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: baseToJobEtaMinutes);

        // Assert
        Assert.Equal(slots1.Count, slots2.Count);
        for (int i = 0; i < slots1.Count; i++)
        {
            Assert.Equal(slots1[i].Window.Start, slots2[i].Window.Start);
            Assert.Equal(slots1[i].Window.End, slots2[i].Window.End);
            Assert.Equal(slots1[i].Type, slots2[i].Type);
            Assert.Equal(slots1[i].Confidence, slots2[i].Confidence);
        }
    }
}


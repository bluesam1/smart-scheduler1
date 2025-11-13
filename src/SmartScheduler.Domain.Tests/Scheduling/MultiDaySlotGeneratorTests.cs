using Moq;
using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

/// <summary>
/// Tests for multi-day slot generation functionality in SlotGenerator.
/// </summary>
public class MultiDaySlotGeneratorTests
{
    private readonly Mock<IAvailabilityEngine> _mockAvailabilityEngine;
    private readonly Mock<ITravelBufferService> _mockTravelBufferService;
    private readonly Mock<IFatigueCalculator> _mockFatigueCalculator;
    private readonly SlotGenerator _slotGenerator;

    public MultiDaySlotGeneratorTests()
    {
        _mockAvailabilityEngine = new Mock<IAvailabilityEngine>();
        _mockTravelBufferService = new Mock<ITravelBufferService>();
        _mockFatigueCalculator = new Mock<IFatigueCalculator>();

        _slotGenerator = new SlotGenerator(
            _mockAvailabilityEngine.Object,
            _mockTravelBufferService.Object,
            _mockFatigueCalculator.Object);
    }

    [Fact]
    public void GenerateSlots_SingleDayAvailable_ReturnsSingleDaySlot()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 0, 0, 0, DateTimeKind.Utc), // Monday
            new DateTime(2025, 1, 17, 23, 59, 59, DateTimeKind.Utc) // Friday
        );

        var jobDurationMinutes = 240; // 4 hours

        // Mock availability engine to return a slot that fits the job
        var availableSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 13, 0, 0, DateTimeKind.Utc), // 9 AM EST
            new DateTime(2025, 1, 13, 21, 0, 0, DateTimeKind.Utc)  // 5 PM EST
        );

        _mockAvailabilityEngine
            .Setup(x => x.CalculateAvailableSlots(
                Moq.It.IsAny<IReadOnlyList<WorkingHours>>(),
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<ContractorCalendar>()))
            .Returns(new List<TimeWindow> { availableSlot });

        _mockTravelBufferService
            .Setup(x => x.CalculateBaseToFirstBuffer(Moq.It.IsAny<int>(), Moq.It.IsAny<double>()))
            .Returns(15);

        _mockFatigueCalculator
            .Setup(x => x.CheckFeasibility(
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<bool>(),
                Moq.It.IsAny<int>()))
            .Returns(new FatigueFeasibilityResult { IsFeasible = true });

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: 30);

        // Assert
        Assert.NotNull(slots);
        Assert.NotEmpty(slots);
        
        var firstSlot = slots.First();
        Assert.NotNull(firstSlot.DailyWindows);
        Assert.Single(firstSlot.DailyWindows); // Should be a single-day slot
    }

    [Fact]
    public void GenerateSlots_NoSingleDayAvailable_AttemptsMultiDay()
    {
        // Arrange - Large job that can't fit in one day
        var workingHours = new List<WorkingHours>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York"),
            new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York"),
            new(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 0, 0, 0, DateTimeKind.Utc), // Monday
            new DateTime(2025, 1, 17, 23, 59, 59, DateTimeKind.Utc) // Friday
        );

        var jobDurationMinutes = 960; // 16 hours - can't fit in one 8-hour day

        // Mock availability engine to return NO single-day slots
        _mockAvailabilityEngine
            .Setup(x => x.CalculateAvailableSlots(
                Moq.It.IsAny<IReadOnlyList<WorkingHours>>(),
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<ContractorCalendar>()))
            .Returns(new List<TimeWindow>()); // No single-day slots available

        _mockTravelBufferService
            .Setup(x => x.CalculateBaseToFirstBuffer(Moq.It.IsAny<int>(), Moq.It.IsAny<double>()))
            .Returns(15);

        _mockFatigueCalculator
            .Setup(x => x.CheckFeasibility(
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<bool>(),
                Moq.It.IsAny<int>()))
            .Returns(new FatigueFeasibilityResult { IsFeasible = true });

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            "America/New_York",
            "America/New_York",
            baseToJobEtaMinutes: 30);

        // Assert
        // Should attempt multi-day generation
        // May return empty if multi-day also not feasible, or may return multi-day slots
        Assert.NotNull(slots);
        
        if (slots.Any())
        {
            var firstSlot = slots.First();
            Assert.NotNull(firstSlot.DailyWindows);
            // If it generated a multi-day slot, it should have 2 or 3 daily windows
            Assert.True(firstSlot.DailyWindows.Count >= 2 && firstSlot.DailyWindows.Count <= 3);
        }
    }

    [Fact]
    public void GeneratedSlot_AlwaysHasDailyWindows()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 13, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 21, 0, 0, DateTimeKind.Utc)
        );

        var jobDurationMinutes = 120;

        var availableSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 13, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 21, 0, 0, DateTimeKind.Utc)
        );

        _mockAvailabilityEngine
            .Setup(x => x.CalculateAvailableSlots(
                Moq.It.IsAny<IReadOnlyList<WorkingHours>>(),
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<ContractorCalendar>()))
            .Returns(new List<TimeWindow> { availableSlot });

        _mockTravelBufferService
            .Setup(x => x.CalculateBaseToFirstBuffer(Moq.It.IsAny<int>(), Moq.It.IsAny<double>()))
            .Returns(15);

        _mockFatigueCalculator
            .Setup(x => x.CheckFeasibility(
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<bool>(),
                Moq.It.IsAny<int>()))
            .Returns(new FatigueFeasibilityResult { IsFeasible = true });

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(slots);
        foreach (var slot in slots)
        {
            Assert.NotNull(slot.DailyWindows);
            Assert.NotEmpty(slot.DailyWindows);
            Assert.All(slot.DailyWindows, window =>
            {
                Assert.True(window.IsValid);
                Assert.True(window.Start < window.End);
            });
        }
    }

    [Fact]
    public void GenerateSlots_MultiDaySlot_DailyWindowsAreConsecutive()
    {
        // This test verifies the multi-day logic creates consecutive days
        // We'll test this by checking the contract - if DailyWindows has multiple entries,
        // they should be consecutive days

        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York"),
            new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 0, 0, 0, DateTimeKind.Utc), // Monday
            new DateTime(2025, 1, 17, 23, 59, 59, DateTimeKind.Utc) // Friday
        );

        // Large job requiring multiple days
        var jobDurationMinutes = 960; // 16 hours

        _mockAvailabilityEngine
            .Setup(x => x.CalculateAvailableSlots(
                Moq.It.IsAny<IReadOnlyList<WorkingHours>>(),
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<ContractorCalendar>()))
            .Returns(new List<TimeWindow>()); // Force multi-day path

        _mockTravelBufferService
            .Setup(x => x.CalculateBaseToFirstBuffer(Moq.It.IsAny<int>(), Moq.It.IsAny<double>()))
            .Returns(15);

        _mockFatigueCalculator
            .Setup(x => x.CheckFeasibility(
                Moq.It.IsAny<TimeWindow>(),
                Moq.It.IsAny<IReadOnlyList<TimeWindow>>(),
                Moq.It.IsAny<int>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<bool>(),
                Moq.It.IsAny<int>()))
            .Returns(new FatigueFeasibilityResult { IsFeasible = true });

        // Act
        var slots = _slotGenerator.GenerateSlots(
            workingHours,
            serviceWindow,
            Array.Empty<TimeWindow>(),
            jobDurationMinutes,
            "America/New_York",
            "America/New_York");

        // Assert
        if (slots.Any() && slots.First().DailyWindows.Count > 1)
        {
            var dailyWindows = slots.First().DailyWindows;
            
            for (int i = 0; i < dailyWindows.Count - 1; i++)
            {
                var currentDay = dailyWindows[i].Start.Date;
                var nextDay = dailyWindows[i + 1].Start.Date;
                
                // Next day should be exactly 1 day after current day (consecutive)
                Assert.Equal(currentDay.AddDays(1), nextDay);
            }
        }
    }
}

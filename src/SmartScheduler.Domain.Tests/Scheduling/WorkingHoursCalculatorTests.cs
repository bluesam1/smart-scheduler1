using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class WorkingHoursCalculatorTests
{
    private readonly IWorkingHoursCalculator _calculator;

    public WorkingHoursCalculatorTests()
    {
        _calculator = new WorkingHoursCalculator();
    }

    [Fact]
    public void CalculateAvailableWindows_WithBasicWorkingHours_ReturnsWindows()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), // Monday 9 AM EST
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc)); // Monday 5 PM EST

        // Act
        var windows = _calculator.CalculateAvailableWindows(
            workingHours,
            serviceWindow,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(windows);
        Assert.All(windows, window =>
        {
            Assert.True(window.Start < window.End);
            Assert.True(window.Start >= serviceWindow.Start);
            Assert.True(window.End <= serviceWindow.End);
        });
    }

    [Fact]
    public void CalculateAvailableWindows_WithHoliday_ExcludesHoliday()
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

        // Act
        var windows = _calculator.CalculateAvailableWindows(
            workingHours,
            serviceWindow,
            "America/New_York",
            "America/New_York",
            calendar);

        // Assert
        Assert.Empty(windows); // No windows on holidays
    }

    [Fact]
    public void CalculateAvailableWindows_WithOverrideException_UsesOverrideHours()
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

        // Act
        var windows = _calculator.CalculateAvailableWindows(
            workingHours,
            serviceWindow,
            "America/New_York",
            "America/New_York",
            calendar);

        // Assert
        Assert.NotEmpty(windows);
        // Windows should be within override hours (10 AM - 2 PM EST = 15:00-19:00 UTC)
        Assert.All(windows, window =>
        {
            Assert.True(window.Start >= new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc));
            Assert.True(window.End <= new DateTime(2025, 1, 13, 19, 0, 0, DateTimeKind.Utc));
        });
    }

    [Fact]
    public void CalculateAvailableWindows_WithMultipleDays_ReturnsWindowsAcrossDays()
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

        // Act
        var windows = _calculator.CalculateAvailableWindows(
            workingHours,
            serviceWindow,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.NotEmpty(windows);
        var mondayWindows = windows.Where(w => w.Start.Date == new DateTime(2025, 1, 13).Date).ToList();
        var tuesdayWindows = windows.Where(w => w.Start.Date == new DateTime(2025, 1, 14).Date).ToList();
        Assert.NotEmpty(mondayWindows);
        Assert.NotEmpty(tuesdayWindows);
    }

    [Fact]
    public void GetWorkingHoursForDate_WithRegularDay_ReturnsWorkingHours()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var date = new DateOnly(2025, 1, 13); // Monday

        // Act
        var result = _calculator.GetWorkingHoursForDate(
            workingHours,
            date,
            "America/New_York");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), result.StartTime);
        Assert.Equal(new TimeOnly(17, 0), result.EndTime);
    }

    [Fact]
    public void GetWorkingHoursForDate_WithHoliday_ReturnsNull()
    {
        // Arrange
        var workingHours = new List<WorkingHours>
        {
            new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
        };

        var calendar = new ContractorCalendar(
            holidays: new List<DateOnly> { new DateOnly(2025, 1, 13) });

        var date = new DateOnly(2025, 1, 13); // Monday (holiday)

        // Act
        var result = _calculator.GetWorkingHoursForDate(
            workingHours,
            date,
            "America/New_York",
            calendar);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetWorkingHoursForDate_WithOverrideException_ReturnsOverrideHours()
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

        var date = new DateOnly(2025, 1, 13); // Monday

        // Act
        var result = _calculator.GetWorkingHoursForDate(
            workingHours,
            date,
            "America/New_York",
            calendar);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new TimeOnly(10, 0), result.StartTime);
        Assert.Equal(new TimeOnly(14, 0), result.EndTime);
    }

    [Fact]
    public void CalculateAvailableWindows_WithNoWorkingHours_ReturnsEmpty()
    {
        // Arrange
        var workingHours = Array.Empty<WorkingHours>();
        var serviceWindow = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 22, 0, 0, DateTimeKind.Utc));

        // Act
        var windows = _calculator.CalculateAvailableWindows(
            workingHours,
            serviceWindow,
            "America/New_York",
            "America/New_York");

        // Assert
        Assert.Empty(windows);
    }
}


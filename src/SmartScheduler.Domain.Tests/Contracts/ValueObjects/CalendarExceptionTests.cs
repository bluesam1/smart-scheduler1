using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.ValueObjects;

public class CalendarExceptionTests
{
    [Fact]
    public void Constructor_WithHolidayType_ShouldCreateInstance()
    {
        // Arrange & Act
        var exception = new CalendarException(
            new DateOnly(2025, 12, 25),
            CalendarExceptionType.Holiday);

        // Assert
        Assert.Equal(new DateOnly(2025, 12, 25), exception.Date);
        Assert.Equal(CalendarExceptionType.Holiday, exception.Type);
        Assert.Null(exception.WorkingHours);
    }

    [Fact]
    public void Constructor_WithOverrideTypeAndWorkingHours_ShouldCreateInstance()
    {
        // Arrange
        var workingHours = new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(10, 0),
            new TimeOnly(14, 0),
            "America/New_York");

        // Act
        var exception = new CalendarException(
            new DateOnly(2025, 1, 1),
            CalendarExceptionType.Override,
            workingHours);

        // Assert
        Assert.Equal(new DateOnly(2025, 1, 1), exception.Date);
        Assert.Equal(CalendarExceptionType.Override, exception.Type);
        Assert.NotNull(exception.WorkingHours);
        Assert.Equal(workingHours, exception.WorkingHours);
    }

    [Fact]
    public void Constructor_WithOverrideTypeWithoutWorkingHours_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new CalendarException(
            new DateOnly(2025, 1, 1),
            CalendarExceptionType.Override,
            null));
    }
}


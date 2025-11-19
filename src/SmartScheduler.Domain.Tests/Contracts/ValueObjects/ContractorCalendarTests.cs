using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.ValueObjects;

public class ContractorCalendarTests
{
    [Fact]
    public void Constructor_WithDefaultValues_ShouldCreateInstance()
    {
        // Arrange & Act
        var calendar = new ContractorCalendar();

        // Assert
        Assert.Empty(calendar.Holidays);
        Assert.Empty(calendar.Exceptions);
        Assert.Equal(30, calendar.DailyBreakMinutes);
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldCreateInstance()
    {
        // Arrange
        var holidays = new List<DateOnly> { new DateOnly(2025, 12, 25) };
        var exceptions = new List<CalendarException>
        {
            new CalendarException(new DateOnly(2025, 1, 1), CalendarExceptionType.Holiday)
        };

        // Act
        var calendar = new ContractorCalendar(
            holidays: holidays,
            exceptions: exceptions,
            dailyBreakMinutes: 60);

        // Assert
        Assert.Single(calendar.Holidays);
        Assert.Single(calendar.Exceptions);
        Assert.Equal(60, calendar.DailyBreakMinutes);
    }

    [Fact]
    public void Constructor_WithNegativeBreakMinutes_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContractorCalendar(
            dailyBreakMinutes: -1));
    }
}





using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.ValueObjects;

public class WorkingHoursTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange & Act
        var workingHours = new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            "America/New_York");

        // Assert
        Assert.Equal(DayOfWeek.Monday, workingHours.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), workingHours.StartTime);
        Assert.Equal(new TimeOnly(17, 0), workingHours.EndTime);
        Assert.Equal("America/New_York", workingHours.TimeZone);
        Assert.Equal(480, workingHours.DurationMinutes);
        Assert.True(workingHours.IsValid);
    }

    [Fact]
    public void Constructor_WithStartTimeAfterEndTime_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(17, 0),
            new TimeOnly(9, 0),
            "America/New_York"));
    }

    [Fact]
    public void Constructor_WithStartTimeEqualToEndTime_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(9, 0),
            "America/New_York"));
    }

    [Fact]
    public void Constructor_WithEmptyTimeZone_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            ""));
    }

    [Fact]
    public void Constructor_WithNullTimeZone_ShouldThrowException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new WorkingHours(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            null!));
    }
}



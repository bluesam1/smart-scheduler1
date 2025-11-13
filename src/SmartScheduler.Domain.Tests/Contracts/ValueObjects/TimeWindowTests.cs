using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.ValueObjects;

public class TimeWindowTests
{
    [Fact]
    public void Constructor_WithValidTimes_ShouldCreateInstance()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddHours(4);

        // Act
        var window = new TimeWindow(start, end);

        // Assert
        Assert.Equal(start, window.Start);
        Assert.Equal(end, window.End);
        Assert.True(window.IsValid);
        Assert.Equal(240, window.DurationMinutes); // 4 hours = 240 minutes
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_ShouldThrowException()
    {
        // Arrange
        var start = DateTime.UtcNow.AddHours(4);
        var end = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TimeWindow(start, end));
    }

    [Fact]
    public void Constructor_WithStartEqualToEnd_ShouldThrowException()
    {
        // Arrange
        var time = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TimeWindow(time, time));
    }

    [Fact]
    public void DurationMinutes_ShouldCalculateCorrectly()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddMinutes(90);

        // Act
        var window = new TimeWindow(start, end);

        // Assert
        Assert.Equal(90, window.DurationMinutes);
    }

    [Fact]
    public void IsValid_WithValidWindow_ShouldReturnTrue()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);

        // Act
        var window = new TimeWindow(start, end);

        // Assert
        Assert.True(window.IsValid);
    }
}


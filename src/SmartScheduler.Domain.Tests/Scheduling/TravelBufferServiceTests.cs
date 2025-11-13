using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class TravelBufferServiceTests
{
    private readonly ITravelBufferService _service;

    public TravelBufferServiceTests()
    {
        _service = new TravelBufferService();
    }

    [Fact]
    public void CalculateBuffer_WithShortETA_ReturnsMinimum()
    {
        // Arrange
        var etaMinutes = 5; // Very short ETA

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(10, buffer); // Minimum 10 minutes
    }

    [Fact]
    public void CalculateBuffer_WithMediumETA_ReturnsCalculatedValue()
    {
        // Arrange
        var etaMinutes = 60; // 60 minutes ETA
        // Expected: 60 × 0.25 = 15 minutes

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(15, buffer);
    }

    [Fact]
    public void CalculateBuffer_WithLongETA_ReturnsMaximum()
    {
        // Arrange
        var etaMinutes = 200; // Very long ETA
        // Expected: 200 × 0.25 = 50, but max is 45

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(45, buffer); // Maximum 45 minutes
    }

    [Fact]
    public void CalculateBuffer_WithExactMaximum_ReturnsMaximum()
    {
        // Arrange
        var etaMinutes = 180; // 180 × 0.25 = 45 (exact maximum)

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(45, buffer);
    }

    [Fact]
    public void CalculateBuffer_WithZeroETA_ReturnsMinimum()
    {
        // Arrange
        var etaMinutes = 0; // Same location

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(10, buffer); // Minimum 10 minutes even for same location
    }

    [Fact]
    public void CalculateBuffer_WithRegionalMultiplier_AppliesMultiplier()
    {
        // Arrange
        var etaMinutes = 60; // 60 × 0.25 = 15
        var regionalMultiplier = 1.5; // 15 × 1.5 = 22.5

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes, regionalMultiplier);

        // Assert
        Assert.Equal(22, buffer); // Rounded to 22 (22.5 rounds down)
    }

    [Fact]
    public void CalculateBuffer_WithRegionalMultiplierExceedingMax_ReturnsMaximum()
    {
        // Arrange
        var etaMinutes = 100; // 100 × 0.25 = 25
        var regionalMultiplier = 2.0; // 25 × 2.0 = 50, but max is 45

        // Act
        var buffer = _service.CalculateBuffer(etaMinutes, regionalMultiplier);

        // Assert
        Assert.Equal(45, buffer); // Capped at maximum
    }

    [Fact]
    public void CalculateBaseToFirstBuffer_ReturnsCorrectBuffer()
    {
        // Arrange
        var etaMinutes = 40; // 40 × 0.25 = 10

        // Act
        var buffer = _service.CalculateBaseToFirstBuffer(etaMinutes);

        // Assert
        Assert.Equal(10, buffer);
    }

    [Fact]
    public void CalculateJobToJobBuffer_ReturnsCorrectBuffer()
    {
        // Arrange
        var etaMinutes = 80; // 80 × 0.25 = 20

        // Act
        var buffer = _service.CalculateJobToJobBuffer(etaMinutes);

        // Assert
        Assert.Equal(20, buffer);
    }

    [Fact]
    public void CalculateLastToBaseBuffer_ReturnsCorrectBuffer()
    {
        // Arrange
        var etaMinutes = 120; // 120 × 0.25 = 30

        // Act
        var buffer = _service.CalculateLastToBaseBuffer(etaMinutes);

        // Assert
        Assert.Equal(30, buffer);
    }

    [Fact]
    public void CalculateBuffer_WithNegativeETA_ThrowsException()
    {
        // Arrange
        var etaMinutes = -10;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CalculateBuffer(etaMinutes));
    }

    [Fact]
    public void CalculateBuffer_WithZeroMultiplier_ThrowsException()
    {
        // Arrange
        var etaMinutes = 60;
        var regionalMultiplier = 0.0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CalculateBuffer(etaMinutes, regionalMultiplier));
    }

    [Theory]
    [InlineData(10, 10)] // Minimum boundary
    [InlineData(40, 10)] // 40 × 0.25 = 10 (exact minimum)
    [InlineData(60, 15)] // 60 × 0.25 = 15
    [InlineData(100, 25)] // 100 × 0.25 = 25
    [InlineData(180, 45)] // 180 × 0.25 = 45 (exact maximum)
    [InlineData(200, 45)] // Maximum boundary
    public void CalculateBuffer_WithVariousETAs_ReturnsExpectedValues(int etaMinutes, int expectedBuffer)
    {
        // Act
        var buffer = _service.CalculateBuffer(etaMinutes);

        // Assert
        Assert.Equal(expectedBuffer, buffer);
    }
}


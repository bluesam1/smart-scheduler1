using SmartScheduler.Domain.Contracts.ValueObjects;
using SmartScheduler.Domain.Scheduling.Services;
using Xunit;

namespace SmartScheduler.Domain.Tests.Scheduling;

public class FatigueCalculatorTests
{
    private readonly IFatigueCalculator _calculator;

    public FatigueCalculatorTests()
    {
        _calculator = new FatigueCalculator();
    }

    [Fact]
    public void CalculateDailyHours_WithNoAssignments_ReturnsZero()
    {
        // Arrange
        var existingAssignments = Array.Empty<TimeWindow>();
        var date = new DateOnly(2025, 1, 13);

        // Act
        var hours = _calculator.CalculateDailyHours(existingAssignments, date, "America/New_York");

        // Assert
        Assert.Equal(0.0, hours);
    }

    [Fact]
    public void CalculateDailyHours_WithAssignments_ReturnsCorrectHours()
    {
        // Arrange
        // 2 hours on Monday Jan 13, 2025 (9 AM - 11 AM EST = 14:00-16:00 UTC)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc))
        };
        var date = new DateOnly(2025, 1, 13);

        // Act
        var hours = _calculator.CalculateDailyHours(existingAssignments, date, "America/New_York");

        // Assert
        Assert.Equal(2.0, hours, 1);
    }

    [Fact]
    public void CheckFeasibility_WithinTargetHours_ReturnsFeasible()
    {
        // Arrange
        var existingAssignments = Array.Empty<TimeWindow>();
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc)); // 2 hours
        var jobDurationMinutes = 120;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York");

        // Assert
        Assert.True(result.IsFeasible);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void CheckFeasibility_ExceedsHardStop_ReturnsNotFeasible()
    {
        // Arrange
        // 11 hours already worked (exceeds 12h hard stop when adding 2 more hours)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 14, 1, 0, 0, DateTimeKind.Utc)) // 11 hours
        };
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 14, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 14, 3, 0, 0, DateTimeKind.Utc)); // 2 hours
        var jobDurationMinutes = 120;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York");

        // Assert
        Assert.False(result.IsFeasible);
        Assert.NotNull(result.Reason);
        Assert.Contains("hard stop", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckFeasibility_ExceedsSoftCap_NonRush_ReturnsNotFeasible()
    {
        // Arrange
        // 9 hours already worked (exceeds 10h soft cap when adding 2 more hours)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 13, 23, 0, 0, DateTimeKind.Utc)) // 9 hours
        };
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 14, 1, 0, 0, DateTimeKind.Utc)); // 2 hours
        var jobDurationMinutes = 120;
        var isRushJob = false;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            isRushJob);

        // Assert
        Assert.False(result.IsFeasible);
        Assert.NotNull(result.Reason);
        Assert.Contains("soft cap", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckFeasibility_ExceedsSoftCap_RushJob_ReturnsFeasible()
    {
        // Arrange
        // 9 hours already worked (exceeds 10h soft cap when adding 2 more hours)
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(
                new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 13, 23, 0, 0, DateTimeKind.Utc)) // 9 hours
        };
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 14, 1, 0, 0, DateTimeKind.Utc)); // 2 hours
        var jobDurationMinutes = 120;
        var isRushJob = true;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York",
            isRushJob);

        // Assert
        Assert.True(result.IsFeasible); // Rush job can bypass soft cap
    }

    [Fact]
    public void CheckFeasibility_ExceedsConsecutiveJobsLimit_ReturnsNotFeasible()
    {
        // Arrange
        // 4 consecutive jobs without break
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 18, 0, 0, DateTimeKind.Utc))
        };
        // 5th job immediately after (no break)
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 19, 0, 0, DateTimeKind.Utc));
        var jobDurationMinutes = 60;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York");

        // Assert
        Assert.False(result.IsFeasible);
        Assert.NotNull(result.Reason);
        Assert.Contains("consecutive", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.RequiredBreakMinutes);
    }

    [Fact]
    public void CheckFeasibility_WithBreakAfterConsecutiveJobs_ReturnsFeasible()
    {
        // Arrange
        // 4 consecutive jobs
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 17, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 18, 0, 0, DateTimeKind.Utc))
        };
        // 5th job with 20 minute break (more than required 15 minutes)
        var proposedSlot = new TimeWindow(
            new DateTime(2025, 1, 13, 18, 20, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 13, 19, 20, 0, DateTimeKind.Utc));
        var jobDurationMinutes = 60;

        // Act
        var result = _calculator.CheckFeasibility(
            proposedSlot,
            existingAssignments,
            jobDurationMinutes,
            "America/New_York");

        // Assert
        Assert.True(result.IsFeasible); // Break is sufficient
    }

    [Fact]
    public void CalculateConsecutiveJobsCount_WithNoAssignments_ReturnsZero()
    {
        // Arrange
        var existingAssignments = Array.Empty<TimeWindow>();
        var beforeTime = new DateTime(2025, 1, 13, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var count = _calculator.CalculateConsecutiveJobsCount(existingAssignments, beforeTime);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CalculateConsecutiveJobsCount_WithBreak_StopsCounting()
    {
        // Arrange
        var existingAssignments = new List<TimeWindow>
        {
            new TimeWindow(new DateTime(2025, 1, 13, 14, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc)),
            new TimeWindow(new DateTime(2025, 1, 13, 15, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 16, 0, 0, DateTimeKind.Utc)),
            // 20 minute break
            new TimeWindow(new DateTime(2025, 1, 13, 16, 20, 0, DateTimeKind.Utc), new DateTime(2025, 1, 13, 17, 20, 0, DateTimeKind.Utc))
        };
        var beforeTime = new DateTime(2025, 1, 13, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var count = _calculator.CalculateConsecutiveJobsCount(existingAssignments, beforeTime);

        // Assert
        Assert.Equal(1, count); // Only the last job (break resets count)
    }
}


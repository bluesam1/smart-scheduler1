using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;
using Xunit;

namespace SmartScheduler.Domain.Tests.Contracts.Entities;

/// <summary>
/// Tests for Job entity's LastRecommendationAuditId functionality.
/// </summary>
public class JobLastRecommendationTests
{
    [Fact]
    public void NewJob_LastRecommendationAuditId_IsNull()
    {
        // Arrange & Act
        var job = CreateTestJob();

        // Assert
        Assert.Null(job.LastRecommendationAuditId);
    }

    [Fact]
    public void UpdateLastRecommendationAuditId_SetsValue()
    {
        // Arrange
        var job = CreateTestJob();
        var auditId = Guid.NewGuid();

        // Act
        job.UpdateLastRecommendationAuditId(auditId);

        // Assert
        Assert.Equal(auditId, job.LastRecommendationAuditId);
    }

    [Fact]
    public void UpdateLastRecommendationAuditId_UpdatesTimestamp()
    {
        // Arrange
        var job = CreateTestJob();
        var originalUpdatedAt = job.UpdatedAt;
        
        // Wait a tiny bit to ensure timestamp changes
        Thread.Sleep(10);

        // Act
        job.UpdateLastRecommendationAuditId(Guid.NewGuid());

        // Assert
        Assert.True(job.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateLastRecommendationAuditId_CanSetToNull()
    {
        // Arrange
        var job = CreateTestJob();
        job.UpdateLastRecommendationAuditId(Guid.NewGuid());
        Assert.NotNull(job.LastRecommendationAuditId);

        // Act
        job.UpdateLastRecommendationAuditId(null);

        // Assert
        Assert.Null(job.LastRecommendationAuditId);
    }

    [Fact]
    public void UpdateLastRecommendationAuditId_CanUpdateMultipleTimes()
    {
        // Arrange
        var job = CreateTestJob();
        var firstAuditId = Guid.NewGuid();
        var secondAuditId = Guid.NewGuid();

        // Act
        job.UpdateLastRecommendationAuditId(firstAuditId);
        Assert.Equal(firstAuditId, job.LastRecommendationAuditId);

        job.UpdateLastRecommendationAuditId(secondAuditId);

        // Assert
        Assert.Equal(secondAuditId, job.LastRecommendationAuditId);
        Assert.NotEqual(firstAuditId, job.LastRecommendationAuditId);
    }

    [Fact]
    public void Job_MaintainsLastRecommendationAuditId_AfterOtherUpdates()
    {
        // Arrange
        var job = CreateTestJob();
        var auditId = Guid.NewGuid();
        job.UpdateLastRecommendationAuditId(auditId);

        // Act - perform other updates
        job.UpdateDescription("Updated description");
        job.UpdatePriority(Priority.High);

        // Assert - LastRecommendationAuditId should remain unchanged
        Assert.Equal(auditId, job.LastRecommendationAuditId);
    }

    private Job CreateTestJob()
    {
        var location = new GeoLocation(
            latitude: 40.7128,
            longitude: -74.0060,
            address: "123 Test St",
            city: "New York",
            state: "NY",
            postalCode: "10001",
            country: "US",
            formattedAddress: "123 Test St, New York, NY 10001",
            placeId: "test-place-id"
        );

        var serviceWindow = new TimeWindow(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4)
        );

        return new Job(
            id: Guid.NewGuid(),
            type: "HVAC Repair",
            duration: 240,
            location: location,
            timezone: "America/New_York",
            serviceWindow: serviceWindow,
            priority: Priority.Normal,
            desiredDate: DateTime.UtcNow.AddDays(1),
            requiredSkills: new[] { "HVAC", "Electrical" },
            description: "Test job description"
        );
    }
}



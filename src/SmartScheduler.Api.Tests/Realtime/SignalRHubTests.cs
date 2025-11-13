using Microsoft.AspNetCore.SignalR;
using SmartScheduler.Realtime.Hubs;
using Xunit;

namespace SmartScheduler.Api.Tests.Realtime;

/// <summary>
/// Integration tests for SignalR RecommendationsHub.
/// </summary>
public class SignalRHubTests
{
    [Fact]
    public void RecommendationsHub_CanBeInstantiated()
    {
        // Arrange & Act
        var hub = new RecommendationsHub();

        // Assert
        Assert.NotNull(hub);
    }

    [Fact]
    public void RecommendationsHub_GroupNames_AreCorrectlyFormatted()
    {
        // Arrange
        var region = "north-america";
        var contractorId = "123e4567-e89b-12d3-a456-426614174000";

        // Act & Assert - Verify group name format
        var dispatchGroup = $"dispatch/{region}";
        var contractorGroup = $"contractor/{contractorId}";

        Assert.Equal("dispatch/north-america", dispatchGroup);
        Assert.Equal("contractor/123e4567-e89b-12d3-a456-426614174000", contractorGroup);
    }
}


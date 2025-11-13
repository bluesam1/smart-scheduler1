using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Recommendations.Services;
using SmartScheduler.Domain.Contracts.ValueObjects;
using Xunit;

namespace SmartScheduler.Application.Tests.Recommendations.Services;

public class TieBreakerServiceTests
{
    private readonly ITieBreakerService _tieBreakerService;

    public TieBreakerServiceTests()
    {
        var logger = new LoggerFactory().CreateLogger<TieBreakerService>();
        _tieBreakerService = new TieBreakerService(logger);
    }

    [Fact]
    public void ApplyTieBreakers_WithEarliestStart_OrdersByStartTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, new List<TimeWindow> { new(now.AddHours(2), now.AddHours(4)) } },
            { contractor2, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } } // Earlier
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.5 },
            { contractor2, 0.5 }
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, 30 },
            { contractor2, 30 }
        };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(contractor2, result[0]); // Earlier start wins
        Assert.Equal(contractor1, result[1]);
    }

    [Fact]
    public void ApplyTieBreakers_WithSameStart_OrdersByUtilization()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } },
            { contractor2, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } } // Same start
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.7 }, // Higher utilization
            { contractor2, 0.3 }  // Lower utilization wins
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, 30 },
            { contractor2, 30 }
        };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(contractor2, result[0]); // Lower utilization wins
        Assert.Equal(contractor1, result[1]);
    }

    [Fact]
    public void ApplyTieBreakers_WithSameStartAndUtilization_OrdersByTravel()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } },
            { contractor2, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } }
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.5 },
            { contractor2, 0.5 }
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, 45 }, // Longer travel
            { contractor2, 20 }  // Shorter travel wins
        };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(contractor2, result[0]); // Shorter travel wins
        Assert.Equal(contractor1, result[1]);
    }

    [Fact]
    public void ApplyTieBreakers_WithNullTravel_HandlesGracefully()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } },
            { contractor2, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } }
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.5 },
            { contractor2, 0.5 }
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, null }, // No travel data
            { contractor2, 20 }    // Has travel data, wins
        };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(contractor2, result[0]); // Has travel data wins over null
        Assert.Equal(contractor1, result[1]);
    }

    [Fact]
    public void ApplyTieBreakers_WithNoSlots_HandlesGracefully()
    {
        // Arrange
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, Array.Empty<TimeWindow>() }, // No slots
            { contractor2, new List<TimeWindow> { new(DateTime.UtcNow, DateTime.UtcNow.AddHours(2)) } }
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.5 },
            { contractor2, 0.5 }
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, 30 },
            { contractor2, 30 }
        };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(contractor2, result[0]); // Has slots wins
        Assert.Equal(contractor1, result[1]);
    }

    [Fact]
    public void ApplyTieBreakers_WithEmptyCandidates_ReturnsEmpty()
    {
        // Arrange
        var candidates = Array.Empty<Guid>();

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(
            candidates,
            new Dictionary<Guid, IReadOnlyList<TimeWindow>>(),
            new Dictionary<Guid, double>(),
            new Dictionary<Guid, int?>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyTieBreakers_WithSingleCandidate_ReturnsSame()
    {
        // Arrange
        var contractor1 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1 };

        // Act
        var result = _tieBreakerService.ApplyTieBreakers(
            candidates,
            new Dictionary<Guid, IReadOnlyList<TimeWindow>>(),
            new Dictionary<Guid, double>(),
            new Dictionary<Guid, int?>());

        // Assert
        Assert.Single(result);
        Assert.Equal(contractor1, result[0]);
    }

    [Fact]
    public void ApplyTieBreakers_IsDeterministic_SameInputsProduceSameOrder()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var contractor1 = Guid.NewGuid();
        var contractor2 = Guid.NewGuid();
        var contractor3 = Guid.NewGuid();
        var candidates = new List<Guid> { contractor1, contractor2, contractor3 };

        var slots = new Dictionary<Guid, IReadOnlyList<TimeWindow>>
        {
            { contractor1, new List<TimeWindow> { new(now.AddHours(2), now.AddHours(4)) } },
            { contractor2, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } },
            { contractor3, new List<TimeWindow> { new(now.AddHours(1), now.AddHours(3)) } }
        };

        var utilization = new Dictionary<Guid, double>
        {
            { contractor1, 0.5 },
            { contractor2, 0.7 },
            { contractor3, 0.3 } // Lower utilization
        };

        var travel = new Dictionary<Guid, int?>
        {
            { contractor1, 30 },
            { contractor2, 40 },
            { contractor3, 20 } // Shortest travel
        };

        // Act
        var result1 = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);
        var result2 = _tieBreakerService.ApplyTieBreakers(candidates, slots, utilization, travel);

        // Assert
        Assert.Equal(result1, result2);
    }
}


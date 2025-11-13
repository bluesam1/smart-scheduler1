using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.Entities;

public class ContractorTests
{
    private GeoLocation CreateValidLocation() => new GeoLocation(40.7128, -74.0060, "123 Main St", "New York", "NY");
    
    private IReadOnlyList<WorkingHours> CreateValidWorkingHours() => new List<WorkingHours>
    {
        new WorkingHours(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York"),
        new WorkingHours(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0), "America/New_York")
    };

    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var location = CreateValidLocation();
        var workingHours = CreateValidWorkingHours();

        // Act
        var contractor = new Contractor(id, "John Doe", location, workingHours);

        // Assert
        Assert.Equal(id, contractor.Id);
        Assert.Equal("John Doe", contractor.Name);
        Assert.Equal(location, contractor.BaseLocation);
        Assert.Equal(50, contractor.Rating); // Default rating
        Assert.Equal(workingHours.Count, contractor.WorkingHours.Count);
        Assert.Empty(contractor.Skills);
        Assert.Equal(4, contractor.MaxJobsPerDay); // Default
        Assert.Single(contractor.DomainEvents);
        Assert.IsType<ContractorCreated>(contractor.DomainEvents[0]);
    }

    [Fact]
    public void Constructor_WithAllProperties_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var location = CreateValidLocation();
        var workingHours = CreateValidWorkingHours();
        var skills = new List<string> { "Hardwood Installation", "Tile" };
        var calendar = new ContractorCalendar();

        // Act
        var contractor = new Contractor(
            id,
            "John Doe",
            location,
            workingHours,
            skills: skills,
            rating: 75,
            calendar: calendar,
            maxJobsPerDay: 6);

        // Assert
        Assert.Equal(75, contractor.Rating);
        Assert.Equal(2, contractor.Skills.Count);
        Assert.Equal("hardwood installation", contractor.Skills[0]); // Normalized
        Assert.Equal("tile", contractor.Skills[1]); // Normalized
        Assert.NotNull(contractor.Calendar);
        Assert.Equal(6, contractor.MaxJobsPerDay);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var workingHours = CreateValidWorkingHours();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Contractor(
            Guid.NewGuid(),
            "",
            location,
            workingHours));
    }

    [Fact]
    public void Constructor_WithNullBaseLocation_ShouldThrowException()
    {
        // Arrange
        var workingHours = CreateValidWorkingHours();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Contractor(
            Guid.NewGuid(),
            "John Doe",
            null!,
            workingHours));
    }

    [Fact]
    public void Constructor_WithInvalidRating_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var workingHours = CreateValidWorkingHours();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Contractor(
            Guid.NewGuid(),
            "John Doe",
            location,
            workingHours,
            rating: 101));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Contractor(
            Guid.NewGuid(),
            "John Doe",
            location,
            workingHours,
            rating: -1));
    }

    [Fact]
    public void Constructor_WithEmptyWorkingHours_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Contractor(
            Guid.NewGuid(),
            "John Doe",
            location,
            Array.Empty<WorkingHours>()));
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdate()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act
        contractor.UpdateName("Jane Doe");

        // Assert
        Assert.Equal("Jane Doe", contractor.Name);
        Assert.Contains(contractor.DomainEvents, e => e is ContractorUpdated);
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contractor.UpdateName(""));
    }

    [Fact]
    public void UpdateRating_WithValidRating_ShouldUpdate()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours(),
            rating: 50);

        // Act
        contractor.UpdateRating(75);

        // Assert
        Assert.Equal(75, contractor.Rating);
        var ratedEvent = contractor.DomainEvents.OfType<ContractorRated>().First();
        Assert.Equal(50, ratedEvent.PreviousRating);
        Assert.Equal(75, ratedEvent.NewRating);
    }

    [Fact]
    public void UpdateRating_WithInvalidRating_ShouldThrowException()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => contractor.UpdateRating(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => contractor.UpdateRating(-1));
    }

    [Fact]
    public void UpdateSkills_ShouldNormalizeSkills()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act
        contractor.UpdateSkills(new List<string> { "  Hardwood Installation  ", "TILE", "carpet" });

        // Assert
        Assert.Equal(3, contractor.Skills.Count);
        Assert.Equal("hardwood installation", contractor.Skills[0]);
        Assert.Equal("tile", contractor.Skills[1]);
        Assert.Equal("carpet", contractor.Skills[2]);
    }

    [Fact]
    public void UpdateSkills_ShouldRemoveDuplicates()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act
        contractor.UpdateSkills(new List<string> { "tile", "Tile", "TILE" });

        // Assert
        Assert.Single(contractor.Skills);
        Assert.Equal("tile", contractor.Skills[0]);
    }

    [Fact]
    public void UpdateBaseLocation_WithValidLocation_ShouldUpdate()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());
        var newLocation = new GeoLocation(34.0522, -118.2437, "456 Oak Ave", "Los Angeles", "CA");

        // Act
        contractor.UpdateBaseLocation(newLocation);

        // Assert
        Assert.Equal(newLocation, contractor.BaseLocation);
    }

    [Fact]
    public void UpdateBaseLocation_WithNullLocation_ShouldThrowException()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => contractor.UpdateBaseLocation(null!));
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var contractor = new Contractor(
            Guid.NewGuid(),
            "John Doe",
            CreateValidLocation(),
            CreateValidWorkingHours());
        contractor.UpdateName("Jane Doe");

        // Act
        contractor.ClearDomainEvents();

        // Assert
        Assert.Empty(contractor.DomainEvents);
    }
}


